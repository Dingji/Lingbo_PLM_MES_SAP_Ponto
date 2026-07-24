# PLM_MES_SAP_Ponto

**PLM → MES → SAP Data Synchronization Bridge**

A .NET 9.0 Windows service solution that bridges **Oracle Agile PLM** (Product Lifecycle Management) to **MES** (Manufacturing Execution System), enabling real-time synchronization of BOM (Bill of Materials) structures and engineering file distribution triggered by Oracle database webhooks.

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Solution Structure](#solution-structure)
- [Projects](#projects)
- [Database Schema](#database-schema)
- [API Endpoints Reference](#api-endpoints-reference)
- [Web Dashboard](#web-dashboard)
- [Oracle Trigger Setup](#oracle-trigger-setup)
- [Configuration](#configuration)
- [Installation & Deployment](#installation--deployment)
- [Security](#security)
- [Logging System](#logging-system)
- [Key Technical Decisions](#key-technical-decisions)
- [Troubleshooting](#troubleshooting)
- [Appendix: MES Payload Schemas](#appendix-mes-payload-schemas)

---

## Overview

**PLM_MES_SAP_Ponto** is a comprehensive integration solution that connects **Oracle Agile PLM** with an external **MES (Manufacturing Execution System)**. It listens for database change events via webhooks triggered by Oracle triggers on BOM and FILES tables, processes the changed data by querying the Agile PLM schema, transforms it into the MES-compatible format, and uploads it to the MES HTTP endpoint.

The system consists of **two cooperating Windows services**:

| Service | Port | Role |
|---------|------|------|
| **PlmMesSync** | 31456 | BOM synchronization orchestrator, trigger receiver, unified web dashboard |
| **FileSyncService** | 31457 | File download, content analysis (MD5, archive extraction), and MES file upload |

Both are self-contained .NET 9.0 applications using ASP.NET Core Minimal APIs, EF Core with Oracle provider, Serilog for structured logging, and a full SPA dashboard.

---

## Architecture

### System Context

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Oracle Agile PLM Database                            │
│  ┌───────┐ ┌───────┐ ┌──────┐ ┌────────┐ ┌─────────┐ ┌──────────┐         │
│  │ ITEM  │ │  BOM  │ │ REV  │ │ CHANGE │ │ REFDESIG│ │LISTENTRY │         │
│  └───────┘ └───┬───┘ └──────┘ └────────┘ └─────────┘ └──────────┘         │
│                │ Trigger: AFTER INSERT/UPDATE/DELETE ON BOM                │
│                │ UTL_HTTP POST → http://127.0.0.1:31456/api/trigger        │
│                ▼                                                          │
│  ┌───────┐ ┌─────────┐ ┌────────┐ ┌──────────────┐ ┌──────────────┐       │
│  │ FILES │ │FILE_INFO│ │VERSION │ │VERSION_FILE_M│ │ATTACHMENT_MAP│       │
│  └───┬───┘ └─────────┘ └────────┘ └──────────────┘ └──────┬───────┘       │
│      │          Trigger: AFTER INSERT/UPDATE ON FILES      │               │
│      └─────────────────────────────────────────────────────┘               │
└─────────────────────────────────────────────────────────────────────────┘
                              │ POST http://127.0.0.1:31456/api/trigger
                              ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                      PlmMesSync — Port 31456                                  │
│                                                                              │
│  POST /api/trigger → TriggerEndpoints (validate, route)                      │
│    ├─ "BOM"   → BomDebouncerService (delay, then SyncBom)                   │
│    └─ "FILES" → FileSyncDebouncerService (delay, then SyncFile)             │
│                                                                              │
│  Dashboard: GET /api/dashboard/triggers|syncs|logs|status                    │
│  Proxy: GET /api/dashboard/filesync/* → proxies to FileSyncService:31457    │
└──────────────────────────────────────────────────────────────────────────────┘
                              │ POST /fileSync (file metadata)
                              ▼
┌──────────────────────────────────────────────────────────────────────────────┐
│                     FileSyncService — Port 31457                              │
│                                                                              │
│  POST /fileSync → 1. Download/Read file  2. MD5 hash  3. Archive detection   │
│                   4. Archive extraction  5. Upload to MES as base64           │
│                                                                              │
│  Dashboard: GET /api/dashboard/requests|status|logs                          │
└──────────────────────────────────────────────────────────────────────────────┘
                              │ POST /api/updateImsData
                              ▼
                    ┌──────────────────────────┐
                    │     MES System Endpoint   │
                    │   10.95.6.37:8888          │
                    └──────────────────────────┘
```

### BOM Synchronization Flow

1. **Oracle Trigger fires** on BOM INSERT/UPDATE/DELETE, calls `UTL_HTTP.POST` to `http://127.0.0.1:31456/api/trigger`
2. **TriggerEndpoints** validates body/table/action fields and routes to `BomDebouncerService.EnqueueTrigger(evt)` → returns `200 OK` immediately
3. **Debouncing**: `ConcurrentDictionary<int, CancellationTokenSource>` tracks pending syncs. If a new trigger for the same BOM ID arrives within the delay window, the previous `CancellationTokenSource` is cancelled and replaced. After the configured delay (default 15 seconds), a DI scope is created and `IBomSyncService.SyncBom()` is called
4. **SyncBom**: Loads BOM → Item → all Revs (ordered by ReleaseDate) → all BOM lines (with RefDesigs). Builds lookup maps (ListEntry for LANGID=4, Change, ComponentRev). For each revision, filters active lines via `IsLineActiveInRev()` (compares CHANGE_IN/CHANGE_OUT revision indices). Builds `BomSyncOutput`. Writes log file to `log/bom/`. Uploads latest revision to MES via `MesUploadService` with retry (3 attempts, exponential backoff at attempt×3s)

### File Synchronization Flow

1. **Oracle Trigger fires** on FILES INSERT/UPDATE → webhook POST to PlmMesSync
2. **FileSyncDebouncerService** debounces, then `FileSyncUploadService.SyncFile(fileId)` traces through Agile schema:
   - `FILES.ID → VERSION_FILE_MAP.FILE_ID → VERSION_FILE_MAP.VERSION_ID`
   - `→ VERSION.ID → VERSION.ATTACH_ID + VERSION_NUM`
   - `→ ATTACHMENT_MAP.ATTACH_ID + VERSION → PARENT_ID`
   - `→ ITEM.ID → ITEM.ITEM_NUMBER`
   - Also: `FILES.FILENAME` + `FILE_INFO.IFS_FILEPATH`
3. Forwards to FileSyncService via `POST /fileSync` with retry (3 attempts)
4. FileSyncService processes (download/read, MD5, archive detection/extraction, MES upload)

---

## Solution Structure

```
PLM_MES_SAP_Ponto/
├── src/
│   ├── PlmMesSync/                  # BOM sync orchestrator (port 31456)
│   │   ├── Program.cs               # Entry point, DI, middleware, Kestrel on :31456
│   │   ├── AppConstants.cs          # Static config constants
│   │   ├── ConfigCipher.cs          # DPAPI encryption/decryption (ENC(...) syntax)
│   │   ├── appsettings.json         # Production configuration
│   │   ├── appsettings.Development.json  # Development overrides
│   │   ├── PlmMesSync.csproj        # .NET 9.0, Oracle EF Core, Serilog
│   │   ├── Data/
│   │   │   ├── AgileDbContext.cs    # EF Core DbContext (11 DbSets)
│   │   │   └── EntityConfigurations/  # 11 IEntityTypeConfiguration<T> files
│   │   ├── Models/
│   │   │   ├── Entities/            # 11 EF Core entity classes
│   │   │   └── Dto/                # 5 DTO classes (BomSyncOutput, MesUploadRequest, etc.)
│   │   ├── Services/                # 7 service classes (BomSync, Debouncers, Stores, etc.)
│   │   ├── Endpoints/               # 3 Minimal API endpoint files
│   │   ├── Logging/                 # BomFileLogger.cs
│   │   ├── Properties/              # launchSettings.json
│   │   └── wwwroot/                 # SPA dashboard (index.html + style.css + app.js)
│   │
│   └── FileSyncService/             # File processor (port 31457)
│       ├── Program.cs               # Entry point, DI, Kestrel on :31457
│       ├── AppConstants.cs          # Static config constants
│       ├── appsettings.json         # Configuration
│       ├── appsettings.Development.json
│       ├── FileSyncService.csproj   # .NET 9.0, SharpCompress, Serilog
│       ├── Models/Dto/              # 5 DTO classes (FileSyncRequest, Response, etc.)
│       ├── Services/                # 5 service classes (FileSyncProcessor, etc.)
│       ├── Endpoints/               # 2 Minimal API endpoint files
│       └── wwwroot/                 # SPA dashboard (index.html + style.css + app.js)
│
├── tests/PlmMesSync.Tests/          # 29 unit tests (xUnit + Moq)
│   ├── ConfigCipherTests.cs         # 3 tests
│   ├── Endpoints/TriggerEndpointsTests.cs  # 6 tests
│   └── Services/                    # 4 test files (FileSyncProcessor, MesUpload, SyncStore, TriggerStore)
│
├── Requirement/                     # Original Chinese requirement documents
│   ├── Requirement.md               # Main requirements + DB schema
│   ├── Requirement2.md              # File sync requirements
│   └── ER_And_SQL.md               # ER diagrams + reference SQL queries
├── log/                             # Runtime log directory
└── PLM_MES_SAP_Ponto.sln
```

---

## Projects

### 1. PlmMesSync (Port 31456)

**Project file:** `src/PlmMesSync/PlmMesSync.csproj`
**Target:** .NET 9.0, self-contained, `win-x64`, `InvariantGlobalization=true`

**NuGet dependencies:**
- `Oracle.EntityFrameworkCore` 9.23.60 — EF Core provider for Oracle
- `Microsoft.Extensions.Hosting.WindowsServices` 9.0.0 — Windows Service integration
- `Serilog.Extensions.Hosting` 9.0.0, `Serilog.Settings.Configuration` 9.0.0, `Serilog.Sinks.Console` 6.0.0, `Serilog.Sinks.File` 6.0.0 — Structured logging
- `System.Security.Cryptography.ProtectedData` 9.0.0 — DPAPI for password encryption

#### Program.cs Startup Sequence

1. **CLI mode:** If `args[0] == "--encrypt"`, encrypts `args[1]` with DPAPI, outputs `ENC(...)`, exits
2. Sets `Directory.SetCurrentDirectory(AppContext.BaseDirectory)`
3. Creates bootstrap Serilog console logger
4. Builds `WebApplication` with custom `ContentRootPath` and `WebRootPath`
5. `UseWindowsService()` with service name `"PLM_MES_SAP_Ponto"`
6. Kestrel on `http://0.0.0.0:31456`
7. Serilog from config
8. **Decrypts connection string:** `ConfigCipher.DecryptConnectionString()` applies regex `/ENC\(([^)]+)\)/` to decrypt each `ENC(...)` token using DPAPI with entropy `"PLM_MES_SAP_Ponto_2024"` and `DataProtectionScope.LocalMachine`
9. Registers `AgileDbContext` with Oracle EF Core
10. Registers all services (see [Services](#services) below)
11. **Database connectivity test:** Creates scope, calls `db.Database.CanConnectAsync()`. On failure, logs error and calls `Environment.Exit(1)`
12. Middleware: `UseDefaultFiles()` + `UseStaticFiles()` + security headers (`X-Content-Type-Options:nosniff`, `X-Frame-Options:DENY`, `Content-Security-Policy`, `X-XSS-Protection:1;mode=block`)
13. Maps endpoints: `MapTriggerEndpoints()`, `MapDashboardEndpoints()`, `MapFileSyncProxyEndpoints()`
14. `app.Run()`

#### Services

| Service | Lifetime | Type | Key Methods | Purpose |
|---------|----------|------|-------------|---------|
| **BomSyncService** | Scoped | `IBomSyncService` | `SyncBom(bomId, triggeredAt)` | Core BOM sync: loads Agile data, resolves active lines per revision, builds output, writes log, uploads to MES |
| **BomDebouncerService** | Singleton | `BackgroundService` | `EnqueueTrigger(evt)`, `PendingCount` | Debounces BOM triggers; accumulates rapid events and executes sync after delay |
| **MesUploadService** | Transient | — | `UploadBomToMes(output)`, `SendWithRetryAsync()` | Uploads BOM JSON to MES with retry (3 attempts, exp. backoff attempt×3s) |
| **FileSyncUploadService** | Transient | — | `SyncFile(fileId, triggeredAt)` | Traces files through Agile schema, forwards to FileSyncService |
| **FileSyncDebouncerService** | Singleton | `BackgroundService` | `EnqueueTrigger(evt)`, `PendingCount` | Debounces FILES triggers, forwards to FileSyncUploadService |
| **TriggerStore** | Singleton | — | `Add()`, `GetRecent(count)`, `TotalCount` | Thread-safe in-memory trigger store, persisted to `log/.trigger_store.json` |
| **SyncStore** | Singleton | — | `Add()`, `GetRecent(count)`, `TotalCount` | Thread-safe in-memory sync store, persisted to `log/.sync_store.json` |
| **BomFileLogger** | Singleton | — | `WriteSyncLog()`, `AppendMesSection()`, `ListLogFiles()`, `ReadLogFile()` | Writes BOM reports to `log/bom/` |

#### Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/trigger` | Receives Oracle trigger webhooks (BOM/FILES routing) |
| GET | `/api/dashboard/triggers?count=N` | Recent trigger records (default: 200) |
| GET | `/api/dashboard/syncs?count=N` | Recent sync execution records (default: 200) |
| GET | `/api/dashboard/logs` | List BOM sync log files (max 100) |
| GET | `/api/dashboard/logs/{filename}` | Read BOM log file content |
| GET | `/api/dashboard/status` | Service health metrics |
| GET | `/api/dashboard/filesync/requests` | Proxied to FileSyncService :31457 |
| GET | `/api/dashboard/filesync/requests/{id}` | Proxied to FileSyncService |
| GET | `/api/dashboard/filesync/status` | Proxied to FileSyncService |
| GET | `/api/dashboard/filesync/logs` | Proxied to FileSyncService |
| GET | `/api/dashboard/filesync/logs/{filename}` | Proxied to FileSyncService |

#### Data Layer (EF Core)

**AgileDbContext** (`Data/AgileDbContext.cs`): 11 `DbSet<T>` properties (Items, Boms, Revs, Changes, RefDesigs, ListEntries, Files, FileInfos, Versions, VersionFileMaps, AttachmentMaps). Applies all 11 `IEntityTypeConfiguration<T>` classes via `ApplyConfigurationsFromAssembly`.

**Entity → Table mapping:**

| Entity | Oracle Table | Key Fields |
|--------|-------------|------------|
| `ItemEntity` | `AGILE.ITEM` | ID, ITEM_NUMBER, DESCRIPTION, DEFAULT_CHANGE |
| `BomEntity` | `AGILE.BOM` | ID, ITEM, ITEM_NUMBER, FIND_NUMBER, QUANTITY, DESCRIPTION, CHANGE_IN, CHANGE_OUT, COMPONENT, LIST06, LIST07. Nav: RefDesigs, ParentItem |
| `RevEntity` | `AGILE.REV` | ID, ITEM, CHANGE, REV_NUMBER, RELEASE_DATE, LATEST_FLAG, DESCRIPTION |
| `ChangeEntity` | `AGILE.CHANGE` | ID, CHANGE_NUMBER, DESCRIPTION, STATUS, RELEASE_DATE |
| `RefDesigEntity` | `AGILE.REFDESIG` | ID, BOM, LABEL |
| `ListEntryEntity` | `AGILE.LISTENTRY` | ID, PARENTID, ENTRYID, ENTRYVALUE, LANGID |
| `FilesEntity` | `AGILE.FILES` | ID, FILENAME |
| `FileInfoEntity` | `AGILE.FILE_INFO` | FILE_ID, IFS_FILEPATH |
| `VersionEntity` | `AGILE.VERSION` | ID, ATTACH_ID, VERSION_NUM |
| `VersionFileMapEntity` | `AGILE.VERSION_FILE_MAP` | ID, VERSION_ID, FILE_ID |
| `AttachmentMapEntity` | `AGILE.ATTACHMENT_MAP` | ID, PARENT_ID, PARENT_ID2, ATTACH_ID, VERSION |

---

### 2. FileSyncService (Port 31457)

**Project file:** `src/FileSyncService/FileSyncService.csproj`
**Target:** .NET 9.0, self-contained, `win-x64`

**NuGet dependencies:**
- `SharpCompress` 0.50.0 — Multi-format archive support (ZIP, RAR, 7z, TAR, GZIP, BZIP2, XZ)
- `Microsoft.Extensions.Hosting.WindowsServices` 9.0.0
- `Serilog.*` — Structured logging

#### Services

| Service | Lifetime | Key Methods | Purpose |
|---------|----------|-------------|---------|
| **FileSyncProcessor** | Singleton | `ProcessFileAsync(item, ct)` | Core file processing: download/read, MD5, archive detection/extraction, path sanitization |
| **MesFileUploadService** | Transient | `UploadFilesToMes(results, ct)` | Uploads processed files to MES; expands archives into individual base64 entries |
| **FileSyncFileLogger** | Singleton | `WriteRequestLog()`, `AppendMesSection()`, `ListLogFiles()` | Writes request logs to `log/requests/REQ_*.log` |
| **LogReaderService** | Singleton | `ListLogFiles()`, `ReadLogFile()` | Log reading for dashboard |
| **RequestStore** | Singleton | `Add()`, `Update()`, `GetRecent()`, `GetById()`, `GetStats()` | Thread-safe store, persisted to `log/.request_store.json` |

**FileSyncProcessor** configuration (from `"FileSyncSettings"`):
- `RootDirectory`: `D:\FileSyncRoot` — root for downloaded files
- `DownloadTimeoutSeconds`: 120 — download HTTP timeout
- `MaxConcurrentDownloads`: 10 — SemaphoreSlim limit for concurrent downloads
- `MaxFileSizeBytes`: 536870912 (512 MB) — maximum allowed file size

**ProcessFileAsync pipeline:**
1. Source: if `RelativePath` provided → read local file (with security check: resolved path must start with `RootDirectory`); else download from `FileUrl` via `"FileSyncDownload"` HttpClient
2. Compute MD5 (lowercase hex) via `MD5.HashDataAsync()`
3. Detect archive type: extension check → fallback to magic bytes (ZIP=504B0304, RAR=52617221, 7Z=377ABCAF, GZIP=1F8B, BZ2=425A68, XZ=FD377A58)
4. If archive: extract via SharpCompress (supports nested archives)
5. All steps recorded with UTC timestamps in `ProcessingStep` list

**Archive types:** ZIP, RAR, 7Z, TAR, GZIP, BZIP2, XZ, TAR.GZ, TAR.BZ2, TAR.XZ

#### Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/fileSync` | Main file sync endpoint |
| GET | `/api/dashboard/requests?count=N` | Recent request records (default: 100) |
| GET | `/api/dashboard/requests/{requestId}` | Single request detail |
| GET | `/api/dashboard/status` | Service statistics |
| GET | `/api/dashboard/logs` | List request log files (max 200) |
| GET | `/api/dashboard/logs/{filename}` | Read log file content |

---

### 3. PlmMesSync.Tests

**Framework:** xUnit 2.9.3 + Moq 4.20.72 | **Target:** .NET 9.0 | **Tests:** 29

| Test Class | Tests | Coverage |
|-----------|-------|----------|
| `ConfigCipherTests` | 3 | DecryptConnectionString: no match, empty, null |
| `TriggerEndpointsTests` | 6 | BOM/FILES routing, unknown table, null body, zero ID |
| `FileSyncProcessorTests` | 10 | SanitizePath (6 cases), SanitizeFileName (4 cases) — security validation |
| `MesUploadServiceTests` | 3 | BOM request mapping, substitute group logic, empty lines |
| `SyncStoreTests` | 3 | Record creation, descending order, total count |
| `TriggerStoreTests` | 4 | Sequence increment, most-recent-first order, max 2000 capacity, value correctness |

---

## Database Schema

### Complete Oracle Table Definitions

The system reads from the `AGILE` schema. Below are complete column definitions for all 12 tables.

#### 1. ITEM — Master Part/Material Catalog

```sql
CREATE TABLE AGILE.ITEM (
    ID                  NUMBER(10)    NOT NULL PRIMARY KEY,
    CLASS               NUMBER(10),           SUBCLASS            NUMBER(10),
    ITEM_NUMBER         VARCHAR2(300) NOT NULL,  -- Unique item code (business key)
    CATEGORY            NUMBER(10),           DESCRIPTION         VARCHAR2(720),
    DOCSIZE             NUMBER(10),           MODIFYDATE          DATE,
    OBJVERSION          NUMBER,               DELETE_FLAG         NUMBER,
    PRODUCT_LINES       VARCHAR2(1024),       FLAGS               VARCHAR2(32),
    DEFAULT_CHANGE      NUMBER       DEFAULT 0,   -- Current active ECO ID
    COMMODITY           NUMBER,               ENCODE_NAME         VARCHAR2(300) NOT NULL UNIQUE,
    PART_FAMILY         NUMBER(10),           CONV_FACTOR         NUMBER(16,4),
    IS_TLA              NUMBER(1),            EXCLUDE_FROM_ROLLUP NUMBER(1) DEFAULT 0,
    ITEM_GROUP          VARCHAR2(765),        LATEST_RELEASED_ECO NUMBER DEFAULT 0 NOT NULL,
    MODEL_REF           VARCHAR2(40),         CREATED             DATE DEFAULT SYSDATE NOT NULL,
    LAST_UPD            DATE DEFAULT SYSDATE NOT NULL,
    FUNC_TEAM           VARCHAR2(765)
);
```

#### 2. BOM — Bill of Materials

```sql
CREATE TABLE AGILE.BOM (
    ID                    NUMBER(10)   NOT NULL PRIMARY KEY,
    ITEM                  NUMBER(10),      -- FK→ITEM.ID (parent item)
    ITEM_NUMBER           VARCHAR2(300),   -- Component item number (denormalized)
    FIND_NUMBER           VARCHAR2(32),    -- Line sort key (e.g., "10", "20")
    SEQ                   NUMBER(5),       QUANTITY  VARCHAR2(40),  -- Usage quantity (string field)
    DESCRIPTION           VARCHAR2(4000),  NOTES     VARCHAR2(1333),
    DATE01..DATE05        DATE,            TEXT01..TEXT05 VARCHAR2(150),
    LIST01..LIST10        NUMBER(10),
    CHANGE_IN             NUMBER(10),      -- FK→CHANGE.ID (ECO that added this line)
    CHANGE_OUT            NUMBER(10),      -- FK→CHANGE.ID (ECO that removed; 0=active)
    PRIOR_BOM             NUMBER(10),      FLAGS VARCHAR2(32) DEFAULT '00000000000000000000000000000000',
    COMPONENT             NUMBER DEFAULT 0,  -- FK→ITEM.ID (component sub-item)
    SITE                  NUMBER DEFAULT 0,
    NUMERIC01..NUMERIC05  NUMBER,
    CREATED DATE DEFAULT SYSDATE NOT NULL, LAST_UPD DATE DEFAULT SYSDATE NOT NULL,
    IS_OPTIONAL NUMBER(1),  IS_MUTUALLY_EXCLUSIVE NUMBER(1),
    MINIMUM_NUMBER NUMBER,  MAXIMUM_NUMBER NUMBER,
    TEXT06..TEXT15 VARCHAR2(150),  LIST11..LIST15 NUMBER(10),
    NUMERIC06..NUMERIC15 NUMBER,  DATE06..DATE15 DATE,
    MULTILIST01..MULTILIST10 VARCHAR2(765)
);
```

**Key fields:** `ITEM` (parent), `ITEM_NUMBER` (component), `FIND_NUMBER` (sort), `QUANTITY` (usage), `CHANGE_IN`/`CHANGE_OUT` (versioning), `COMPONENT` (sub-item), `LIST06` (substitute group, FK→LISTENTRY.ENTRYID), `LIST07` (substitute priority, FK→LISTENTRY.ENTRYID).

#### 3. REV — Item Revisions

```sql
CREATE TABLE AGILE.REV (
    ID                   NUMBER(10)   NOT NULL PRIMARY KEY,
    ITEM                 NUMBER(10),    -- FK→ITEM.ID
    CHANGE               NUMBER(10),    -- FK→CHANGE.ID
    REV_NUMBER           VARCHAR2(40),  -- "R1", "R2", "R3", ...
    OLD_REVNUMBER        VARCHAR2(40),  OBSOLETE_DATE DATE,
    EFFECTIVE_DATE       DATE,          FUNCTION_ID NUMBER(10),
    INCORP_DATE          DATE,          LOC01..LOC10 NUMBER(10),
    DATE01..DATE20       DATE,          TEXT01..TEXT15 VARCHAR2(150),
    LIST01..LIST25       NUMBER(10),    RELEASED NUMBER(5),
    INCORPORATED         NUMBER(5),     LATEST_FLAG NUMBER(5),  -- 1=latest revision
    RELEASE_DATE         DATE,          -- Official release date (used for version ordering)
    FLAGS                VARCHAR2(32),  TEXT06..TEXT15 VARCHAR2(150),
    OLD_RELEASE_TYPE     NUMBER(10),    SITE NUMBER DEFAULT 0,
    NUMERIC01..NUMERIC05 NUMBER,
    DESCRIPTION          VARCHAR2(720), -- Revision description (used as BOM line description)
    -- Plus: WEIGHT, WEIGHT_UOM, MONEYVALUE01-05, MONEYCURRENCY01-05, COMPLIANCY,
    -- AI_MULTILIST01-15, FOLDER_OWNER, VERSION_ID, ATTACHMENT, etc. (60+ columns)
    CREATED DATE DEFAULT SYSDATE NOT NULL,
    LAST_UPD DATE DEFAULT SYSDATE NOT NULL
);
```

#### 4. CHANGE — Engineering Change Orders

```sql
CREATE TABLE AGILE.CHANGE (
    ID                 NUMBER(10)   NOT NULL PRIMARY KEY,
    CLASS              NUMBER(10),      SUBCLASS           NUMBER(10),
    CHANGE_NUMBER      VARCHAR2(90),    -- ECO number (e.g., "ECO-2024-001")
    CATEGORY           NUMBER(10),      STATUS             NUMBER(10),
    REASON_CODE        NUMBER(10),      ORIGINATOR         NUMBER(10),
    OWNER              NUMBER(10),      CREATE_DATE        DATE,
    RESUME_DATE        DATE,            EFFECTIVE_FROM     DATE,
    EFFECTIVE_TO       DATE,            RELEASE_DATE       DATE,
    DESCRIPTION        VARCHAR2(4000),  REASON             VARCHAR2(4000),
    TRANSFERRED        VARCHAR2(128),   MODIFYDATE         DATE,
    OBJVERSION         NUMBER,          DELETE_FLAG        NUMBER,
    SUBMIT_DATE        DATE,            ROUTE_DATE         DATE,
    PRODUCT_LINES      VARCHAR2(255),   FLAGS              VARCHAR2(32),
    WORKFLOW_ID        NUMBER,          STATUSTYPE         NUMBER,
    FINALCOMPLETE_DATE DATE,            PROCESS_ID         NUMBER,
    IN_REVIEW          NUMBER,          ENCODE_NAME        VARCHAR2(300) NOT NULL UNIQUE,
    FUNC_TEAM          VARCHAR2(765),   ROUTED_DATE        DATE
);
```

#### 5. REFDESIG — Placement Reference Designators

```sql
CREATE TABLE AGILE.REFDESIG (
    ID       NUMBER(10)   NOT NULL PRIMARY KEY,
    BOM      NUMBER(10),    -- FK→BOM.ID
    LABEL    VARCHAR2(40),  -- Designator (e.g., "U1", "R2", "C3", "J1")
    CREATED  DATE DEFAULT SYSDATE NOT NULL,
    LAST_UPD DATE DEFAULT SYSDATE NOT NULL
);
```

#### 6. LISTENTRY — List/Enumeration Values

```sql
CREATE TABLE AGILE.LISTENTRY (
    PARENTID     NUMBER,          ACTIVE       NUMBER,
    ENTRYID      NUMBER,          ENTRYVALUE   VARCHAR2(2048),  -- Display value
    LANGID       NUMBER NOT NULL, -- Language ID (4 = English / valid for substitute values)
    DESCRIPTION  VARCHAR2(2048),  PARENT_ENTRY NUMBER,
    ID           NUMBER DEFAULT 0 NOT NULL,  APINAME VARCHAR2(2048),
    READONLY     NUMBER(1) DEFAULT 0,
    CREATED      DATE DEFAULT SYSDATE NOT NULL,
    LAST_UPD     DATE DEFAULT SYSDATE NOT NULL
);
```

**Critical:** Only `LANGID = 4` records are valid. Maps `BOM.LIST06` (substitute group) and `BOM.LIST07` (substitute priority) `ENTRYID` values to human-readable `ENTRYVALUE` names.

#### 7-12. File-Related Tables

```sql
-- FILES — File metadata
CREATE TABLE AGILE.FILES (
    ID                   NUMBER       NOT NULL PRIMARY KEY,
    FILE_SIZE            NUMBER,      FILE_TYPE            VARCHAR2(450),
    FILENAME             VARCHAR2(4000),  -- Original filename
    PAGE                 NUMBER,      FLAGS                VARCHAR2(32),
    UUID                 VARCHAR2(128),   CONTENT_URL          VARCHAR2(1024),
    FILE_FORMAT          VARCHAR2(10),    CONTENT_URL_TEMPLATE VARCHAR2(1024),
    CATEGORY             NUMBER,
    CREATED              DATE DEFAULT SYSDATE NOT NULL,
    LAST_UPD             DATE DEFAULT SYSDATE NOT NULL
);

-- FILE_INFO — File storage paths
CREATE TABLE AGILE.FILE_INFO (
    FILE_ID        NUMBER       NOT NULL PRIMARY KEY,  -- FK→FILES.ID
    FILE_TYPE      VARCHAR2(450),   CHECKSUM_VALUE NUMBER,
    FILE_PATH      VARCHAR2(4000),  LOCATIONS      VARCHAR2(4000),
    EIFS_FILEPATH  VARCHAR2(4000),  IFS_FILEPATH   VARCHAR2(4000),  -- Relative storage path
    HFS_FILEPATH   VARCHAR2(4000),
    CREATED        DATE DEFAULT SYSDATE NOT NULL,
    LAST_UPD       DATE DEFAULT SYSDATE NOT NULL
);

-- VERSION — File versions
CREATE TABLE AGILE.VERSION (
    ID                 NUMBER       NOT NULL PRIMARY KEY,
    ATTACH_ID          NUMBER,         -- FK→ATTACHMENT.ID
    VERSION_NUM        NUMBER,         -- Version number (1, 2, 3...)
    FLAGS              VARCHAR2(32),   CREATE_DATE        DATE,
    LIFECYCLEPHASE     NUMBER,         CHECKIN_USER       NUMBER,
    LAST_UPD           DATE DEFAULT SYSDATE NOT NULL,
    LABEL              VARCHAR2(150),  REVISION           VARCHAR2(150),
    VER_DATE           DATE,           APPROVAL_STATUS    NUMBER(1),
    ITEM_CHANGE_STATUS VARCHAR2(10),   FOLDER_REVISION    VARCHAR2(150),
    CHANGE             NUMBER DEFAULT 0,  INCORPORATED   NUMBER(1) DEFAULT 0,
    CHANGESEQ          VARCHAR2(20),   DATE16..DATE20     DATE,
    LIST26..LIST30     NUMBER(10),     MONEYVALUE11..15   NUMBER(18,6),
    MONEYCURRENCY11..15 NUMBER(4),     MULTILIST16..20    VARCHAR2(765),
    NUMERIC11..15      NUMBER,         TEXT26..TEXT30     VARCHAR2(150)
);

-- VERSION_FILE_MAP — Version-to-file mapping
CREATE TABLE AGILE.VERSION_FILE_MAP (
    ID                NUMBER       NOT NULL PRIMARY KEY,
    VERSION_ID        NUMBER,          -- FK→VERSION.ID
    PATH              VARCHAR2(1024),  DESCRIPTION       VARCHAR2(300),
    LAST_VIEWED       DATE,            FLAGS VARCHAR2(32) DEFAULT '00000000000000000000000000000000',
    DATE01..DATE05    DATE,            LIST01..LIST05    NUMBER(10),
    MULTILIST01..03   VARCHAR2(255),   NUMERIC01..05     NUMBER,
    TEXT01..TEXT06    VARCHAR2(150),   FILE_ID           NUMBER,  -- FK→FILES.ID
    CHECKOUT_LOCATION VARCHAR2(3000),  MASTER_THUMBNAIL  NUMBER,
    CREATED           DATE DEFAULT SYSDATE NOT NULL,
    LAST_UPD          DATE DEFAULT SYSDATE NOT NULL
);

-- ATTACHMENT_MAP — Item-to-attachment mapping
CREATE TABLE AGILE.ATTACHMENT_MAP (
    ID             NUMBER       NOT NULL PRIMARY KEY,
    PARENT_ID      NUMBER,          -- FK→ITEM.ID (via PARENT_ID2 in practice)
    PARENT_ID2     NUMBER,          -- Secondary parent ID (used for ITEM lookup)
    ATTACH_ID      NUMBER,          -- FK→ATTACHMENT.ID
    VERSION        NUMBER,          -- Version (matches VERSION.VERSION_NUM)
    PARENT_CLASS   NUMBER,          DATE01..DATE05 DATE,
    TEXT01..TEXT25 VARCHAR2(150),   LIST01..LIST25 NUMBER(10),
    MULTILIST01..03 VARCHAR2(255),  NUMERIC01..05  NUMBER,
    FLAGS          VARCHAR2(32) DEFAULT '00000000000000000000000000000000',
    VERSION_ID     NUMBER,          ATTACHMENTTYPE NUMBER,
    FILE_ID        NUMBER,          LATEST_VSN     NUMBER,
    CREATED        DATE DEFAULT SYSDATE NOT NULL,
    LAST_UPD       DATE DEFAULT SYSDATE NOT NULL
);

-- ATTACHMENT — Attachment metadata
CREATE TABLE AGILE.ATTACHMENT (
    ID                NUMBER       NOT NULL PRIMARY KEY,
    CLASS             NUMBER,      SUBCLASS          NUMBER,
    ATTACHMENT_NUMBER VARCHAR2(150) NOT NULL,
    OBJVERSION        NUMBER,      DELETE_FLAG       NUMBER,
    LATEST_VSN        NUMBER,      DESCRIPTION       VARCHAR2(4000),
    CHECKOUT_USER     NUMBER,      CHECKOUT_DATE     DATE,
    CHECKOUT_FOLDER   VARCHAR2(3000),  FLAGS         VARCHAR2(32),
    LAST_MOD          DATE,        CREATE_DATE       DATE,
    BASEATTACH_ID     NUMBER,      ATTACHMENTTYPE    NUMBER,
    COMPONENT_TYPE    NUMBER,      DEFAULT_CHANGE    NUMBER DEFAULT 0,
    REDLINE_USER      NUMBER,      ROUTED_DATE       DATE
);
```

### Entity Relationship Diagram

```
╔═══════════════════════════════════════════════════════════════════════════════════════════════════════╗
║                                                                                                       ║
║  ┌──────────────────────────────────────────────────────────────────┐                                 ║
║  │  ╔══════════════════════════╗         ╔══════════════════════════╗  │                                 ║
║  │  ║         ITEM             ║         ║        CHANGE            ║  │                                 ║
║  │  ╠══════════════════════════╣         ╠══════════════════════════╣  │                                 ║
║  │  ║  ID (PK)        NUMBER   ║──┐      ║  ID (PK)        NUMBER   ║  │                                 ║
║  │  ║  ITEM_NUMBER    VARCHAR2 ║  │      ║  CHANGE_NUMBER  VARCHAR2 ║  │                                 ║
║  │  ║  DESCRIPTION    VARCHAR2 ║  │      ║  STATUS         NUMBER   ║  │                                 ║
║  │  ║  DEFAULT_CHANGE NUMBER ──╫──┼──────║  RELEASE_DATE   DATE     ║  │                                 ║
║  │  ║  CLASS          NUMBER   ║  │      ║  DESCRIPTION    VARCHAR2 ║  │                                 ║
║  │  ║  SUBCLASS       NUMBER   ║  │      ║  CREATE_DATE    DATE     ║  │                                 ║
║  │  ║  CATEGORY       NUMBER   ║  │      ╚══════════════════════════╝  │                                 ║
║  │  ║  PRODUCT_LINES  VARCHAR2 ║  │                                    │                                 ║
║  │  ║  PART_FAMILY    NUMBER   ║  │        1                           │                                 ║
║  │  ║  COMMODITY      NUMBER   ║  │        │                           │                                 ║
║  │  ║  IS_TLA         NUMBER   ║  │        │                           │                                 ║
║  │  ║  CREATED        DATE     ║  │        │ N                         │                                 ║
║  │  ║  LAST_UPD       DATE     ║  │        ▼                           │                                 ║
║  │  ╚══════════════════════════╝  │  ┌────────────────────────────────┐ │                                 ║
║  │           │                    │  │  ITEM ─── REV ─── CHANGE       │ │                                 ║
║  │           │ 1                  │  │  (Composite FK: ITEM + CHANGE) │ │                                 ║
║  │           │                    │  └────────────────────────────────┘ │                                 ║
║  │           │ N                  │                                    │                                 ║
║  │           ▼                    │                                    │                                 ║
║  │  ╔══════════════════════════════════════════════════════╗           │                                 ║
║  │  ║                    BOM                              ║           │                                 ║
║  │  ╠══════════════════════════════════════════════════════╣           │                                 ║
║  │  ║  ID (PK)          NUMBER                            ║           │                                 ║
║  │  ║  ITEM (FK) ───────────────→ ITEM.ID                 ║           │                                 ║
║  │  ║  ITEM_NUMBER      VARCHAR2 (Component item code)    ║           │                                 ║
║  │  ║  FIND_NUMBER      VARCHAR2 (Sort key: "10","20")    ║           │                                 ║
║  │  ║  QUANTITY         VARCHAR2 (Usage qty, string)      ║           │                                 ║
║  │  ║  DESCRIPTION      VARCHAR2                          ║           │                                 ║
║  │  ║  CHANGE_IN (FK) ─────────→ CHANGE.ID (ECO that added)║          │                                 ║
║  │  ║  CHANGE_OUT(FK) ─────────→ CHANGE.ID (ECO that removed)║         │                                 ║
║  │  ║  COMPONENT (FK) ─────────→ ITEM.ID (Sub-item)       ║           │                                 ║
║  │  ║  LIST06 (FK) ────┐                                 ║           │                                 ║
║  │  ║  LIST07 (FK) ────┤                                 ║           │                                 ║
║  │  ║  SEQ            NUMBER                              ║           │                                 ║
║  │  ║  PRIOR_BOM      NUMBER                              ║           │                                 ║
║  │  ║  FLAGS          VARCHAR2                            ║           │                                 ║
║  │  ║  SITE           NUMBER                              ║           │                                 ║
║  │  ║  IS_OPTIONAL    NUMBER(1)                           ║           │                                 ║
║  │  ║  CREATED        DATE                                ║           │                                 ║
║  │  ║  LAST_UPD       DATE                                ║           │                                 ║
║  │  ╚══════════════════════════════════════════════════════╝           │                                 ║
║  │           │                                                    │  │                                 ║
║  │           │ 1                                                  │  │                                 ║
║  │           │                                                    │  │                                 ║
║  │           │ N                                                  │  │                                 ║
║  │           ▼                                                    │  │                                 ║
║  │  ┌──────────────────────────────────────────────────────────┐  │  │                                 ║
║  │  │  ╔══════════════════════════╗  ╔══════════════════════╗  │  │  │                                 ║
║  │  │  ║       REFDESIG           ║  ║      LISTENTRY       ║  │  │  │                                 ║
║  │  │  ╠══════════════════════════╣  ╠══════════════════════╣  │  │  │                                 ║
║  │  │  ║  ID (PK)        NUMBER   ║  ║  ID          NUMBER ║  │  │  │                                 ║
║  │  │  ║  BOM (FK) ──────────────╫──╫──→ BOM.ID         ║  │  │  │                                 ║
║  │  │  ║  LABEL          VARCHAR2 ║  ║  ENTRYID     NUMBER║  │  │  │                                 ║
║  │  │  ║  CREATED        DATE     ║  ║  ENTRYVALUE VARCHAR2║  │  │  │                                 ║
║  │  │  ║  LAST_UPD       DATE     ║  ║  LANGID    NUMBER  ║  │  │  │                                 ║
║  │  │  ╚══════════════════════════╝  ║  PARENTID   NUMBER ║  │  │  │                                 ║
║  │  │                                ║  PARENT_ENTRY NUMBER║  │  │  │                                 ║
║  │  │                                ║  ACTIVE     NUMBER ║  │  │  │                                 ║
║  │  │  BOM.LIST06 ──→ LISTENTRY.ENTRYID  ║─────────╝     │  │  │                                 ║
║  │  │  BOM.LIST07 ──→ LISTENTRY.ENTRYID  / (LANGID = 4)  │  │  │                                 ║
║  │  │                                └──────────────────────┘  │  │                                 ║
║  │  └──────────────────────────────────────────────────────────┘  │                                 ║
║  │                                                               │                                 ║
║  │  ╔══════════════════════════╗                                 │                                 ║
║  │  ║           REV            ║                                 │                                 ║
║  │  ╠══════════════════════════╣                                 │                                 ║
║  │  ║  ID (PK)        NUMBER   ║                                 │                                 ║
║  │  ║  ITEM (FK) ─────────────╫──→ ITEM.ID                      │                                 ║
║  │  ║  CHANGE (FK) ───────────╫──→ CHANGE.ID                    │                                 ║
║  │  ║  REV_NUMBER     VARCHAR2║  (e.g. "R1","R2","R3")          │                                 ║
║  │  ║  RELEASE_DATE   DATE    ║  (Version ordering)              │                                 ║
║  │  ║  LATEST_FLAG    NUMBER  ║  (1 = latest)                   │                                 ║
║  │  ║  DESCRIPTION    VARCHAR2║  (Used as BOM line description)  │                                 ║
║  │  ║  OLD_REVNUMBER VARCHAR2 ║                                   │                                 ║
║  │  ║  EFFECTIVE_DATE DATE    ║                                   │                                 ║
║  │  ║  CREATED        DATE    ║                                   │                                 ║
║  │  ║  LAST_UPD       DATE    ║                                   │                                 ║
║  │  ╚══════════════════════════╝                                   │                                 ║
║  └──────────────────────────────────────────────────────────────────┘                                 ║
║                                                                                                       ║
║  ═══════════════════════════════════════════════════════════════════════════════════════════════════  ║
║  FILE TRACEABILITY SUB-SCHEMA                                                                        ║
║  ═══════════════════════════════════════════════════════════════════════════════════════════════════  ║
║                                                                                                       ║
║  ┌───────────────────────────────────────────────────────────────────────────────────────────────┐  ║
║  │                                                                                               │  ║
║  │  ITEM.ID ←── ATTACHMENT_MAP.PARENT_ID2 (via ATTACH_ID + VERSION matching)                    │  ║
║  │                                                                                               │  ║
║  │  ╔══════════════════════════╗           ╔══════════════════════════╗                           │  ║
║  │  ║     ATTACHMENT_MAP       ║           ║        VERSION           ║                           │  ║
║  │  ╠══════════════════════════╣           ╠══════════════════════════╣                           │  ║
║  │  ║  ID (PK)         NUMBER  ║           ║  ID (PK)        NUMBER   ║                           │  ║
║  │  ║  PARENT_ID       NUMBER  ║           ║  ATTACH_ID      NUMBER   ║                           │  ║
║  │  ║  PARENT_ID2 ────────────╫───→ ITEM   ║  VERSION_NUM    NUMBER   ║                           │  ║
║  │  ║  ATTACH_ID ───────┐     ║           ║  CREATE_DATE    DATE     ║                           │  ║
║  │  ║  VERSION ─────────┤     ║           ║  LABEL          VARCHAR2 ║                           │  ║
║  │  ║  PARENT_CLASS     NUMBER ║           ║  CHECKIN_USER   NUMBER   ║                           │  ║
║  │  ║  ATTACHMENTTYPE   NUMBER ║           ║  LIFECYCLEPHASE NUMBER   ║                           │  ║
║  │  ║  FILE_ID          NUMBER ║           ╚══════════════════════════╝                           │  ║
║  │  ║  LATEST_VSN       NUMBER ║                         │                                       │  ║
║  │  ║  CREATED          DATE   ║                         │ ID                                     │  ║
║  │  ║  LAST_UPD         DATE   ║                         │                                       │  ║
║  │  ╚══════════════════════════╝                         │                                       │  ║
║  │        │  (ATTACH_ID + VERSION)                       │                                       │  ║
║  │        └─────────── MATCH ──────────────────┘         │                                       │  ║
║  │                                                       │                                       │  ║
║  │                                                       ▼                                       │  ║
║  │  ╔══════════════════════════╗           ╔══════════════════════════╗                           │  ║
║  │  ║    VERSION_FILE_MAP      ║           ║         FILES            ║                           │  ║
║  │  ╠══════════════════════════╣           ╠══════════════════════════╣                           │  ║
║  │  ║  ID (PK)        NUMBER   ║           ║  ID (PK)        NUMBER   ║                           │  ║
║  │  ║  VERSION_ID (FK) ────────────→ VERSION║  FILENAME  VARCHAR2(4000)║                           │  ║
║  │  ║  FILE_ID (FK) ───────────────────→ FILES║ FILE_SIZE   NUMBER     ║                           │  ║
║  │  ║  PATH           VARCHAR2 ║           ║  FILE_TYPE   VARCHAR2    ║                           │  ║
║  │  ║  DESCRIPTION    VARCHAR2 ║           ║  UUID        VARCHAR2    ║                           │  ║
║  │  ║  LAST_VIEWED    DATE     ║           ║  CATEGORY    NUMBER      ║                           │  ║
║  │  ║  CHECKOUT_LOC   VARCHAR2 ║           ║  CREATED     DATE        ║                           │  ║
║  │  ║  CREATED        DATE     ║           ║  LAST_UPD    DATE        ║                           │  ║
║  │  ║  LAST_UPD       DATE     ║           ╚══════════════════════════╝                           │  ║
║  │  ╚══════════════════════════╝                         │                                       │  ║
║  │                                                       │ FILE_ID                                │  ║
║  │                                                       │                                       │  ║
║  │                                                       ▼                                       │  ║
║  │  ╔══════════════════════════╗                                                                  │  ║
║  │  ║        FILE_INFO         ║                                                                  │  ║
║  │  ╠══════════════════════════╣                                                                  │  ║
║  │  ║  FILE_ID (PK,FK)───→ FILES.ID║                                                                  │  ║
║  │  ║  IFS_FILEPATH VARCHAR2 ║  (Relative storage path)                                          │  ║
║  │  ║  FILE_PATH     VARCHAR2║                                                                  │  ║
║  │  ║  EIFS_FILEPATH VARCHAR2║                                                                  │  ║
║  │  ║  HFS_FILEPATH  VARCHAR2║                                                                  │  ║
║  │  ║  FILE_TYPE     VARCHAR2║                                                                  │  ║
║  │  ║  CHECKSUM_VALUE NUMBER ║                                                                  │  ║
║  │  ║  CREATED       DATE    ║                                                                  │  ║
║  │  ║  LAST_UPD      DATE    ║                                                                  │  ║
║  │  ╚══════════════════════════╝                                                                  │  ║
║  │                                                                                               │  ║
║  └───────────────────────────────────────────────────────────────────────────────────────────────┘  ║
║                                                                                                       ║
╚═══════════════════════════════════════════════════════════════════════════════════════════════════════╝
```

**Relationship Summary Table:**

| # | Parent Table | Parent Key | Child Table | Child Key | Cardinality | Description |
|---|-------------|-----------|-------------|-----------|-------------|-------------|
| 1 | **ITEM** | ID | **BOM** | ITEM | 1:N | One item has many BOM lines |
| 2 | **ITEM** | ID | **REV** | ITEM | 1:N | One item has many revisions |
| 3 | **ITEM** | DEFAULT_CHANGE | **CHANGE** | ID | N:1 | Item's current active ECO |
| 4 | **CHANGE** | ID | **BOM** | CHANGE_IN | 1:N | ECO that introduced a BOM line |
| 5 | **CHANGE** | ID | **BOM** | CHANGE_OUT | 1:N | ECO that removed a BOM line |
| 6 | **CHANGE** | ID | **REV** | CHANGE | 1:N | ECO associated with a revision |
| 7 | **BOM** | ID | **REFDESIG** | BOM | 1:N | One BOM line has many placement labels |
| 8 | **BOM** | LIST06 | **LISTENTRY** | ENTRYID | N:1 | Substitute group lookup (LANGID=4) |
| 9 | **BOM** | LIST07 | **LISTENTRY** | ENTRYID | N:1 | Substitute priority lookup (LANGID=4) |
| 10 | **BOM** | COMPONENT | **ITEM** | ID | N:1 | Component sub-item reference |
| 11 | **ITEM** | ID | **ATTACHMENT_MAP** | PARENT_ID2 | 1:N | One item has many file attachments |
| 12 | **ATTACHMENT_MAP** | ATTACH_ID + VERSION | **VERSION** | ATTACH_ID + VERSION_NUM | N:1 | Map attachment version to file version |
| 13 | **VERSION** | ID | **VERSION_FILE_MAP** | VERSION_ID | 1:N | One version has many file entries |
| 14 | **VERSION_FILE_MAP** | FILE_ID | **FILES** | ID | N:1 | Map version entry to file record |
| 15 | **FILES** | ID | **FILE_INFO** | FILE_ID | 1:1 | One file has one storage info record |
| 16 | **REV** | ITEM + CHANGE | — | — | Composite | Revision identified by (Item, Change) pair |

### BOM Active-Line Versioning Algorithm

Implemented in `BomSyncService.IsLineActiveInRev()`:

```
Given: BOM line, current revision index (currentRevIdx), all revisions list,
       and a map of CHANGE.ID → REV entity (revChangeMap)

1. Find when the line was ADDED:
   - If line.ChangeIn is null/0 → addedAtIdx = 0 (always present)
   - Else look up ChangeIn in revChangeMap → addedAtIdx = index of that REV in allRevs
   - If no match → addedAtIdx = 0

2. If currentRevIdx < addedAtIdx → return FALSE (line not yet introduced)

3. Find when the line was REMOVED:
   - If line.ChangeOut is null/0 → never removed (always active)
   - Else look up ChangeOut in revChangeMap → removedAtIdx = index of that REV
   - If no match → never removed

4. If removedAtIdx >= 0 AND currentRevIdx >= removedAtIdx → return FALSE

5. Return TRUE (line is active in this revision)

Example:
  Revisions: [R1(idx=0), R2(idx=1), R3(idx=2), R4(idx=3)]
  Line A: ChangeIn=ECO-001→R1, ChangeOut=ECO-003→R3
  R1: added=0, removed=2, 0≥0=T, 0≥2=F → ACTIVE
  R2: added=0, removed=2, 1≥0=T, 1≥2=F → ACTIVE
  R3: added=0, removed=2, 2≥0=T, 2≥2=T → NOT ACTIVE
  R4: added=0, removed=2, 3≥0=T, 3≥2=T → NOT ACTIVE
```

### Substitute Group Resolution Logic

Implemented in `MesUploadService.BuildRequest()`:

```
FIRST PASS (build primary material map):
  For each line with SubstituteGroup and SubstitutePriority == "1":
    mainCodeByGroup[SubstituteGroup] = line.ItemNumber

SECOND PASS (build MES material list):
  For each line:
    - No SubstituteGroup → IsMain="y", MainCode=self
    - SubstitutePriority == "1" → IsMain="y", MainCode=self
    - Other (alternate) → IsMain="n", MainCode=mainCodeByGroup[group]
```

### File Traceability Chain

```
FILES.ID = fileId
  → VERSION_FILE_MAP (WHERE FILE_ID = fileId) → VERSION_ID
  → VERSION (WHERE ID = VERSION_ID) → ATTACH_ID, VERSION_NUM
  → ATTACHMENT_MAP (WHERE ATTACH_ID = ATTACH_ID AND VERSION = VERSION_NUM) → PARENT_ID
  → ITEM (WHERE ID = PARENT_ID) → ITEM_NUMBER
  Also: FILES.FILENAME, FILE_INFO.IFS_FILEPATH (via FILE_ID)
```

---

## API Endpoints Reference

### PlmMesSync Endpoints

#### POST /api/trigger

Receives Oracle trigger webhook notifications.

**Request:**
```json
{"id": 1234, "action": "INSERT", "table": "BOM"}
```

**Response (200 OK):**
```json
{"message": "accepted", "table": "BOM", "id": 1234, "action": "INSERT"}
```

**Error responses:**
- `400` (null body): `{"error": "Request body is required"}`
- `400` (empty table): `{"error": "'table' field is required"}`
- `400` (empty action): `{"error": "'action' field is required"}`
- `200` (ignored table): `{"message": "ignored", "reason": "table 'OTHER' not handled"}`

**Routing:** `"BOM"` → BomDebouncerService, `"FILES"` → FileSyncDebouncerService, others → ignored.

#### GET /api/dashboard/status

```json
{
  "serviceName": "PLM_MES_SAP_Ponto",
  "status": "running",
  "startTime": "2026-07-21T10:00:00",
  "uptime": "12h 34m 56s",
  "totalTriggers": 1024,
  "totalSyncs": 512,
  "pendingSyncs": 0
}
```

#### GET /api/dashboard/triggers?count=200

Returns JSON array of `TriggerRecord` objects: `[{sequence, bomId, action, table, receivedAt}]`.

#### GET /api/dashboard/syncs?count=200

Returns JSON array of `SyncRecord` objects: `[{sequence, bomId, itemId, itemNumber, triggeredAt, executedAt, status, logFile, error, mesStatus, table}]`.

#### GET /api/dashboard/logs

Returns JSON array of BOM log filenames (max 100, newest first).

#### GET /api/dashboard/logs/{filename}

Returns plain text log content, or `404 {"error":"File not found"}`.

#### GET /api/dashboard/filesync/* (Proxy)

All proxied to FileSyncService:31457. Routes return `502` with error if proxy fails.

### FileSyncService Endpoints

#### POST /fileSync

Main file processing endpoint.

**Request:**
```json
{
  "files": [{
    "inventoryCode": "KK70000010",
    "fileCode": "File01",
    "fileName": "spec.pdf",
    "remark": "",
    "fileUrl": "http://server/spec.pdf",
    "relativePath": ""
  }]
}
```

**Response (200 OK):**
```json
{
  "requestId": "a1b2c3d4e5f6",
  "receivedTimeUtc": "...",
  "completedTimeUtc": "...",
  "durationMs": 5234,
  "status": "Success",
  "totalFiles": 1,
  "successCount": 1,
  "failedCount": 0,
  "results": [{
    "inventoryCode": "KK70000010",
    "fileCode": "File01",
    "fileName": "spec.pdf",
    "sizeBytes": 1048576,
    "md5": "d41d8cd98f00b204e9800998ecf8427e",
    "isZipFile": false,
    "success": true,
    "steps": [{"timestampUtc":"...","step":"REQUEST_RECEIVED","detail":"..."}]
  }]
}
```

**Status:** `"Success"`, `"PartialSuccess"`, `"Failed"`

#### GET /api/dashboard/requests?count=100

Returns `{"success":true,"data":[RequestRecord,...]}`.

#### GET /api/dashboard/requests/{requestId}

Returns `{"success":true,"data":RequestRecord}` or `404`.

#### GET /api/dashboard/status

Returns `{"success":true,"data":{"service":"FileSyncService","port":31457,"uptime":123.45,"totalRequests":50,"successRequests":40,"failedRequests":10,"processingRequests":0,"serverTimeUtc":"..."}}`.

#### GET /api/dashboard/logs

Returns JSON array of filenames (max 200).

#### GET /api/dashboard/logs/{filename}

Returns plain text log content.

---

## Web Dashboard

Both services include a single-page application (SPA) dashboard with vanilla JavaScript, HTML5, and CSS3 (no framework dependencies). Dark theme with responsive design.

### PlmMesSync Dashboard (http://localhost:31456)

| Tab | Features |
|-----|----------|
| **Overview** | Summary cards (total triggers/syncs, MES success/failure, pending counts). Recent activity feed combining triggers and syncs |
| **Triggers** | Paginated table (100/page): sequence, BOM ID, action, table, received time |
| **Syncs** | Paginated table: sequence, BOM ID, item number, status (colored badge), triggered/executed times, log link, MES status |
| **Logs** | File sidebar (sorted by date) + viewer. Parses plaintext BOM reports into formatted HTML tables. MES section with expandable JSON bodies |
| **File Requests** | Proxied view of FileSyncService requests (via `/api/dashboard/filesync/*`) |
| **File Logs** | Proxied view of FileSyncService logs |

**Auto-refresh:** 3 seconds (file sections: 5 seconds). Status badges: green=SUCCESS, red=FAILED, yellow=SKIPPED, blue=Processing.

### FileSyncService Dashboard (http://localhost:31457)

| Tab | Features |
|-----|----------|
| **Overview** | Statistics cards (total/success/failed/processing/uptime) + recent activity table |
| **Requests** | Full history table with status badges. Detail modal: per-file cards, step timeline with elapsed times, ZIP entry viewer, MES upload log |
| **Logs** | Sidebar (200 files max) + content viewer |

**Auto-refresh:** 5 seconds. Base64 `file_content` sanitized for display performance.

---

## Oracle Trigger Setup

### BOM Table Trigger

```sql
CREATE OR REPLACE PROCEDURE send_bom_change(p_id IN NUMBER, p_action IN VARCHAR2) IS
  PRAGMA AUTONOMOUS_TRANSACTION;
  v_req        UTL_HTTP.REQ;
  v_resp       UTL_HTTP.RESP;
  v_url        VARCHAR2(200) := 'http://127.0.0.1:31456/api/trigger';
  v_post_data  VARCHAR2(500);
BEGIN
  v_post_data := '{"id":' || p_id || ',"action":"' || p_action || '","table":"BOM"}';
  v_req := UTL_HTTP.BEGIN_REQUEST(url => v_url, method => 'POST', http_version => 'HTTP/1.1');
  UTL_HTTP.SET_HEADER(v_req, 'Content-Type', 'application/json; charset=UTF-8');
  UTL_HTTP.SET_HEADER(v_req, 'Content-Length', LENGTHB(v_post_data));
  UTL_HTTP.WRITE_TEXT(v_req, v_post_data);
  v_resp := UTL_HTTP.GET_RESPONSE(v_req);
  BEGIN UTL_HTTP.END_RESPONSE(v_resp); EXCEPTION WHEN OTHERS THEN NULL; END;
  COMMIT;
EXCEPTION WHEN OTHERS THEN ROLLBACK;
END send_bom_change;
/

CREATE OR REPLACE TRIGGER trg_bom_change
  AFTER INSERT OR UPDATE OR DELETE ON BOM
  FOR EACH ROW
BEGIN
  IF INSERTING THEN send_bom_change(:NEW.ID, 'INSERT');
  ELSIF UPDATING THEN send_bom_change(:NEW.ID, 'UPDATE');
  END IF;
END;
/
```

### FILES Table Trigger

```sql
CREATE OR REPLACE PROCEDURE send_files_change(p_id IN NUMBER, p_action IN VARCHAR2) IS
  PRAGMA AUTONOMOUS_TRANSACTION;
  v_req        UTL_HTTP.REQ;
  v_resp       UTL_HTTP.RESP;
  v_url        VARCHAR2(200) := 'http://127.0.0.1:31456/api/trigger';
  v_post_data  VARCHAR2(500);
BEGIN
  v_post_data := '{"id":' || p_id || ',"action":"' || p_action || '","table":"FILES"}';
  v_req := UTL_HTTP.BEGIN_REQUEST(url => v_url, method => 'POST', http_version => 'HTTP/1.1');
  UTL_HTTP.SET_HEADER(v_req, 'Content-Type', 'application/json; charset=UTF-8');
  UTL_HTTP.SET_HEADER(v_req, 'Content-Length', LENGTHB(v_post_data));
  UTL_HTTP.WRITE_TEXT(v_req, v_post_data);
  v_resp := UTL_HTTP.GET_RESPONSE(v_req);
  BEGIN UTL_HTTP.END_RESPONSE(v_resp); EXCEPTION WHEN OTHERS THEN NULL; END;
  COMMIT;
EXCEPTION WHEN OTHERS THEN ROLLBACK;
END send_files_change;
/

CREATE OR REPLACE TRIGGER trg_files_change
  AFTER INSERT OR UPDATE OR DELETE ON FILES
  FOR EACH ROW
BEGIN
  IF INSERTING THEN send_files_change(:NEW.ID, 'INSERT');
  ELSIF UPDATING THEN send_files_change(:NEW.ID, 'UPDATE');
  END IF;
END;
/
```

---

## Configuration

### PlmMesSync appsettings.json

```json
{
  "ConnectionStrings": {
    "AgileDb": "Data Source=127.0.0.1:1521/agile9;User Id=agile;Password=ENC(<encrypted>);"
  },
  "SyncSettings": {
    "DelaySeconds": 15,
    "LogBasePath": "log",
    "MesEndpoint": "http://10.170.9.17:8888/ims-integrate/api/updateImsData"
  },
  "FileSyncSettings": {
    "FileSyncServiceUrl": "http://10.170.9.4:31457/fileSync",
    "FileSyncDashboardUrl": "http://10.170.9.4:31457",
    "DelaySeconds": 15
  },
  "Serilog": {
    "MinimumLevel": { "Default": "Information" },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "log/app-.log", "rollingInterval": "Day" } }
    ]
  }
}
```

### FileSyncService appsettings.json

```json
{
  "FileSyncSettings": {
    "RootDirectory": "D:\\FileSyncRoot",
    "DownloadTimeoutSeconds": 120,
    "MaxConcurrentDownloads": 10,
    "MaxFileSizeBytes": 536870912
  },
  "MesUploadSettings": {
    "MesEndpoint": "http://10.170.9.17:8888/ims-integrate/api/updateImsData",
    "TimeoutSeconds": 120
  },
  "Serilog": {
    "MinimumLevel": { "Default": "Information" },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "log/app-.log", "rollingInterval": "Day" } }
    ]
  }
}
```

### Development Overrides

Both services set `Serilog.MinimumLevel.Default: "Debug"` in Development mode. PlmMesSync also enables EF Core sensitive data logging and detailed errors.

---

## Installation & Deployment

### Prerequisites

- Windows Server 2016+ or Windows 10/11
- .NET 9.0 Runtime (or use self-contained deployment)
- Oracle Database 12c network access
- MES endpoint access (`http://10.95.6.37:8888`)
- Oracle UTL_HTTP configured with appropriate ACL grants

### Build & Publish

```powershell
# Publish PlmMesSync (self-contained)
dotnet publish src/PlmMesSync/PlmMesSync.csproj -c Release -r win-x64 --self-contained true -o publish/PlmMesSync

# Publish FileSyncService
dotnet publish src/FileSyncService/FileSyncService.csproj -c Release -r win-x64 --self-contained true -o publish/FileSyncService
```

### Install as Windows Service

```powershell
sc.exe create "PLM_MES_SAP_Ponto" binPath="D:\services\PlmMesSync\PlmMesSync.exe" start=auto
sc.exe description "PLM_MES_SAP_Ponto" "PLM to MES BOM and File Synchronization Service"

sc.exe create "PLM_FileSyncService" binPath="D:\services\FileSyncService\FileSyncService.exe" start=auto
sc.exe description "PLM_FileSyncService" "PLM File Download and MES Upload Service"
```

### Password Encryption with DPAPI

```powershell
# Run on the target machine (DPAPI is machine-specific)
PlmMesSync.exe --encrypt <plaintext_password>
# Output: ENC(AQAAANCMnd8BFdERjHoAwE/...)

# Insert into appsettings.json:
# "Password=ENC(AQAAANCMnd8BFdERjHoAwE/...)"
```

### Verifying Installation

1. Check service status: `sc.exe query "PLM_MES_SAP_Ponto"`
2. Access dashboards: `http://localhost:31456` and `http://localhost:31457`
3. Check startup logs in `log/app-*.log`
4. Simulate trigger: `curl -X POST http://localhost:31456/api/trigger -H "Content-Type: application/json" -d '{"id":1,"action":"INSERT","table":"BOM"}'`

---

## Security

### Path Traversal Prevention
FileSyncProcessor removes `..` iteratively, replaces `/` and `\` with `_`, removes invalid path/file chars. Empty results default to `"_default"` (paths) or `"unnamed_file"` (filenames). Local file mode verifies resolved path is within `RootDirectory`.

### Connection String Encryption
DPAPI `ProtectedData.Protect` with `DataProtectionScope.LocalMachine`. Entropy: `"PLM_MES_SAP_Ponto_2024"`. Machine-bound — `--encrypt` must run on the same machine.

### File Size Limits
Download: `Content-Length` checked before streaming (max 512 MB). Upload: same limit enforced for base64 content. Streaming uses 80KB chunks.

### Concurrent Download Throttling
`SemaphoreSlim(MaxConcurrentDownloads)` — default 10 concurrent downloads.

### HTTP Security Headers
`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'`, `X-XSS-Protection: 1; mode=block`.

### Input Validation
Dashboard count parameters capped. Trigger events validated (non-null body, non-empty table/action). Log file reader uses `Path.GetFileName()` to prevent traversal. All HTTP clients have configured timeouts (10s-120s).

---

## Logging System

### Log Locations

| Service | Log Type | Path | Retention |
|---------|----------|------|-----------|
| PlmMesSync | Application | `log/app-YYYYMMDD.log` (Serilog, daily) | 30 days |
| PlmMesSync | BOM Reports | `log/bom/BOM_{item}_{timestamp}.log` | Manual |
| PlmMesSync | Trigger Store | `log/.trigger_store.json` | 2000 records |
| PlmMesSync | Sync Store | `log/.sync_store.json` | 2000 records |
| FileSyncService | Application | `log/app-YYYYMMDD.log` (Serilog, daily) | 30 days |
| FileSyncService | Request Logs | `log/requests/REQ_{id}_{timestamp}.log` | Manual |
| FileSyncService | Request Store | `log/.request_store.json` | 500 records |

### BOM Report Format

```
================================================================
  BOM Sync Report (Latest Version Only)
  Generated: 2026-07-21 10:00:15
================================================================
  Parent Item:    10141-0L3X-0100
  Description:    Main Board Assembly
================================================================

--- Version: R3 [LATEST] | Released: 2026-07-20 | Lines: 5 ---

  Lvl  Find# | Item Number          | Qty    | Rev    | Description     | RefDesig    | SubGrp  | SubPri | ChgIn      | ChgOut
  ----------------------------------------------------------------------------------------------------------------------------
  1       10 | 20001-AB-001         | 1      | R2     | Power Module    | U1,U2,U3    | GroupA  | 1      | ECO-001    | ECO-003
  1       20 | 20002-IJ-005         | 1      | R1     | Connector       | J1          | -       | -      | ECO-001    | -
...
================================================================
  MES Upload Log
================================================================
  Endpoint:       http://10.170.9.17:8888/ims-integrate/api/updateImsData
  Request Time:   2026-07-21 10:00:15.123
  Response Time:  2026-07-21 10:00:15.456
  Duration:       333ms
  HTTP Status:    200
--- Request Body ---
{"docType":"BS_BOM",...}
--- Response Body ---
{"success":true}
```

### FileSync Request Log Format

Detailed log per request: header (ID, timestamps, IP, status), received request body, per-file details (result, step timeline with elapsed ms, file metadata, MD5, ZIP contents), MES upload sections (base64 content sanitized: `[BASE64 N chars omitted]`).

### In-Memory Store Persistence

| Store | File | Format |
|-------|------|--------|
| TriggerStore | `log/.trigger_store.json` | `{"Sequence":N,"Records":[...]}` |
| SyncStore | `log/.sync_store.json` | `{"Sequence":N,"Records":[...]}` |
| RequestStore | `log/.request_store.json` | `[...RequestRecord]` |

Each loads on construction, saves on every mutation. Corrupted files start fresh silently.

---

## Key Technical Decisions

1. **Minimal API (No MVC Controllers):** All endpoints use `MapGet`/`MapPost` with static delegates. Reduces boilerplate, keeps logic self-contained.

2. **Debouncer Pattern (BackgroundService + ConcurrentDictionary):** Accumulates rapid trigger events (common during ECO releases). Cancels pending timers for the same ID and restarts the delay. Prevents redundant processing.

3. **In-Memory Stores with JSON Persistence:** Thread-safe `ConcurrentDictionary`/`ConcurrentQueue` backed by JSON files. Sub-millisecond reads for dashboard, survives restarts, handles corruption gracefully.

4. **Separate Processes for BOM and File Sync:** FileSyncService runs independently. File operations (download, MD5, archive extraction, base64) are CPU/I/O intensive. Separation prevents blocking BOM sync and allows independent scaling.

5. **SharpCompress for Multi-Format Archives:** Supports 9+ formats (ZIP, RAR, 7z, TAR, GZIP, BZIP2, XZ, TAR.GZ, TAR.BZ2, TAR.XZ) with unified API. `System.IO.Compression` only handles ZIP and GZIP.

6. **DPAPI for Secrets Management:** Machine-bound, no key management infrastructure required. `--encrypt` must run on the target machine.

7. **Oracle EF Core with Fluent Configuration:** 11 dedicated `IEntityTypeConfiguration<T>` classes map Agile PLM's legacy Oracle schema (uppercase, 60+ column tables) to .NET entities.

8. **Proxy Pattern for Unified Dashboard:** PlmMesSync proxies `/api/dashboard/filesync/*` to FileSyncService. Single dashboard URL (`http://localhost:31456`) aggregates both services' data.

9. **Dual-Mode File Resolution:** URL download + local path read modes. Local mode includes directory traversal prevention.

10. **Processing Step Timelines:** Every file operation records UTC-timestamped steps. Enables precise debugging in API responses and log files.

11. **Retry with Exponential Backoff:** 3 attempts, delay = attempt × 3s. Retries on 5xx, `HttpRequestException`, `TaskCanceledException`.

12. **Substitute Group Resolution:** Within each substitute group, priority "1" is primary (`IsMain=y`); others are alternates (`IsMain=n`) referencing the primary's code.

---

## Troubleshooting

| Symptom | Cause | Resolution |
|---------|-------|------------|
| Service won't start | Invalid connection string / DPAPI failure | Verify password encrypted with `--encrypt` on same machine |
| Service exits on startup | Database connection failed | Check Oracle network, credentials, TNS; verify masked connection string in startup log |
| Trigger received but sync never runs | Debouncer delay / shutdown | Check `SyncSettings:DelaySeconds`; verify service uptime |
| MES upload fails | Network / endpoint down / invalid payload | Check `SyncSettings:MesEndpoint`; review MES response in sync log |
| File download fails | URL unreachable / file too large / timeout | Check `DownloadTimeoutSeconds`, `MaxFileSizeBytes` |
| Dashboard shows no data | Store files corrupt / just started | Check store JSON files exist and are valid |
| Archive extraction fails | Corrupted / unsupported format | SharpCompress supports ZIP, RAR, 7z, TAR, GZIP, BZIP2, XZ |
| Oracle triggers not reaching service | UTL_HTTP / ACL not configured | Test `UTL_HTTP.REQUEST('http://ip:31456/api/trigger') FROM DUAL` |
| Proxy returns 502 | FileSyncService unreachable | Start FileSyncService; check network; verify `FileSyncDashboardUrl` |
| Weak portal symptoms | All lines show "No active lines" | Check BOM lines' CHANGE_IN/CHANGE_OUT values and REV records |

---

## Appendix: MES Payload Schemas

### BOM Upload (BS_BOM)

`POST http://{mes}:8888/ims-integrate/api/updateImsData`

```json
{
  "docType": "BS_BOM",
  "updateType": "UPDATE",
  "data": [{
    "org_code": "3701",
    "prod_code": "10141-0L3X-0100",
    "bom_ver": "R3",
    "is_def": "y",
    "is_valid": "y",
    "remark": "Main Board Assembly",
    "bs_bom_mtrl": [{
      "mtrl_code": "20001-AB-001",
      "is_main": "y",
      "main_code": "20001-AB-001",
      "dosage": 1,
      "point_str": "U1,U2,U3",
      "mbom_ver": "R2",
      "remark": "Power Module"
    }]
  }]
}
```

| Field | Description | Example |
|-------|-------------|---------|
| `org_code` | Organization code | `"3701"` |
| `prod_code` | Product item number | `"10141-0L3X-0100"` |
| `bom_ver` | BOM version (latest revision) | `"R3"` |
| `is_main` | Primary material flag | `"y"` (primary), `"n"` (alternate) |
| `main_code` | Primary material code (self if primary, group primary if alternate) | `"20001-AB-001"` |
| `dosage` | Usage quantity | `1` |
| `point_str` | Placement positions (comma-separated RefDesig) | `"U1,U2,U3"` |
| `mbom_ver` | Component BOM version | `"R2"` |

### File Upload (CUST_FILE)

`POST http://{mes}:8888/ims-integrate/api/updateImsData`

```json
{
  "docType": "CUST_FILE",
  "updateType": "UPDATE",
  "data": [{
    "ORG_ID": "3701",
    "MTRL_CODE": "KK70000010",
    "file_list": [{
      "file_code": "File01",
      "file_name": "specification",
      "remark": "",
      "file_url": "Engineering/specification.pdf",
      "file_content": "JVBERi0xLjQK...(base64)",
      "file_type": "1"
    }]
  }]
}
```

| Field | Description | Example |
|-------|-------------|---------|
| `ORG_ID` | Organization ID | `"3701"` |
| `MTRL_CODE` | Material/inventory code | `"KK70000010"` |
| `file_code` | Sequential file code | `"File01"`, `"File02"` |
| `file_name` | File name (without extension) | `"specification"` |
| `file_content` | Base64-encoded file content | `"JVBERi0xLjQK..."` |
| `file_type` | File type classification | `"1"` (PDF), `"2"` (other) |
