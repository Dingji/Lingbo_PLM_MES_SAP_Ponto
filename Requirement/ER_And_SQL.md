# PLM_MES_SAP_Ponto - 实体关系与SQL参考

## 一、实体关系图 (ER Diagram)

```
┌─────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                         │
│  ┌──────────────────────┐         ┌──────────────────────┐                              │
│  │        ITEM          │         │       CHANGE         │                              │
│  ├──────────────────────┤         ├──────────────────────┤                              │
│  │ * ID (PK)           │?───┐    │ * ID (PK)           │?───┐                          │
│  │   CLASS             │    │    │   CLASS             │    │                          │
│  │   SUBCLASS          │    │    │   SUBCLASS          │    │                          │
│  │   ITEM_NUMBER       │    │    │   CHANGE_NUMBER     │    │                          │
│  │   DESCRIPTION       │    │    │   STATUS            │    │                          │
│  │   DEFAULT_CHANGE ───┼────┼───?│   CREATE_DATE       │    │                          │
│  │   ...               │    │    │   RELEASE_DATE      │    │                          │
│  └──────────────────────┘    │    │   DESCRIPTION       │    │                          │
│           │                  │    └──────────────────────┘    │                          │
│           │                  │              │                 │                          │
│           │ 1                │              │ 1               │                          │
│           │                  │              │                 │                          │
│           │ N                │              │ N               │                          │
│           ▼                  │              ▼                 │                          │
│  ┌──────────────────────┐    │    ┌──────────────────────┐    │                          │
│  │         REV          │    │    │         BOM          │    │                          │
│  ├──────────────────────┤    │    ├──────────────────────┤    │                          │
│  │ * ID (PK)           │    │    │ * ID (PK)           │    │                          │
│  │   ITEM ─────────────┼────┘    │   ITEM ─────────────┼────┘ (FK → ITEM.ID)          │
│  │   CHANGE ───────────┼────────?│   ITEM_NUMBER       │                               │
│  │   REV_NUMBER        │         │   FIND_NUMBER       │                               │
│  │   OLD_REVNUMBER     │         │   SEQ               │                               │
│  │   RELEASE_DATE      │         │   QUANTITY          │                               │
│  │   LATEST_FLAG       │         │   DESCRIPTION       │                               │
│  │   DESCRIPTION       │         │   CHANGE_IN ────────┼──────? (FK → CHANGE.ID)       │
│  │   ...               │         │   CHANGE_OUT ───────┼──────? (FK → CHANGE.ID)       │
│  └──────────────────────┘         │   LIST06 ───────────┼──────? (FK → LISTENTRY.ENTRYID)│
│                                   │   LIST07 ───────────┼──────? (FK → LISTENTRY.ENTRYID)│
│                                   │   FLAGS             │                               │
│                                   │   ...               │                               │
│                                   └──────────────────────┘                               │
│                                            │                                            │
│                                            │ 1                                          │
│                                            │                                            │
│                                            │ N                                          │
│                                            ▼                                            │
│                                   ┌──────────────────────┐                              │
│                                   │      REFDESIG        │                              │
│                                   ├──────────────────────┤                              │
│                                   │ * ID (PK)           │                              │
│                                   │   BOM ──────────────┼──? (FK → BOM.ID)             │
│                                   │   LABEL             │                              │
│                                   │   CREATED           │                              │
│                                   │   LAST_UPD          │                              │
│                                   └──────────────────────┘                              │
│                                                                                         │
│                                   ┌──────────────────────┐                              │
│                                   │     LISTENTRY        │                              │
│                                   ├──────────────────────┤                              │
│                                   │ * ID (PK)           │                              │
│                                   │   PARENTID          │                              │
│                                   │   ACTIVE            │                              │
│                                   │   ENTRYID ?─────────┼── (BOM.LIST06/LIST07 引用)   │
│                                   │   ENTRYVALUE        │                              │
│                                   │   LANGID (=4 有效)  │                              │
│                                   │   DESCRIPTION       │                              │
│                                   └──────────────────────┘                              │
│                                                                                         │
└─────────────────────────────────────────────────────────────────────────────────────────┘
```

## 二、关系说明

| 关系 | 说明 |
|------|------|
| ITEM.ID ← BOM.ITEM | 一个Item拥有多条BOM行 |
| ITEM.DEFAULT_CHANGE → CHANGE.ID | Item当前关联的Change |
| REV.ITEM → ITEM.ID | 一个Item有多个Rev版本 |
| REV.CHANGE → CHANGE.ID | 每个Rev对应一个Change |
| BOM.CHANGE_IN → CHANGE.ID | 该BOM行在哪个Change被加入 |
| BOM.CHANGE_OUT → CHANGE.ID | 该BOM行在哪个Change被移除 (0=未移除) |
| BOM.ID ← REFDESIG.BOM | 一条BOM行有多个贴片位号 |
| BOM.LIST06 → LISTENTRY.ENTRYID | 替代组别 (LANGID=4有效) |
| BOM.LIST07 → LISTENTRY.ENTRYID | 替代优先级 (LANGID=4有效) |

## 三、版本判定逻辑

对于某个Item的某个Rev版本，一条BOM行是否"活跃"：

1. 通过 `BOM.CHANGE_IN` + `REV.ITEM` + `REV.CHANGE` 找到该BOM行被**加入**的Rev
2. 通过 `BOM.CHANGE_OUT` + `REV.ITEM` + `REV.CHANGE` 找到该BOM行被**移除**的Rev
3. 判定规则：
   - 当前Rev >= 加入Rev → 可见
   - 当前Rev >= 移除Rev → 不可见（已移除）
   - CHANGE_OUT = 0 或 NULL → 从未移除，后续所有版本都可见

---

## 四、SQL：多级BOM树形展开（指定Item，所有版本）

> 递归展开到最低层级。顶层Item显示所有Rev版本，子级物料使用其最新版本展开。
> 同一父项+版本组合只在第一行显示，后续行留空（报表分组样式）。
> 兼容 Oracle 12.1+，最大展开10层防止循环。

```sql
WITH bom_tree (
    lvl, sort_path,
    parent_item_number, parent_desc,
    rev_number, release_date, is_latest,
    find_number, component_item, quantity, component_desc,
    refdesig_labels, sub_group, sub_priority,
    change_in_number, change_out_number,
    component_item_id
) AS (
    -- ===== 锚点：顶层Item，按每个Rev版本展开 =====
    SELECT
        1,
        TO_CHAR(r.RELEASE_DATE, 'YYYYMMDD') || '.' ||
            LPAD(REGEXP_REPLACE(b.FIND_NUMBER, '[^0-9]', ''), 4, '0'),
        i.ITEM_NUMBER,
        i.DESCRIPTION,
        r.REV_NUMBER,
        r.RELEASE_DATE,
        CASE WHEN r.LATEST_FLAG = 1 THEN 'Y' ELSE 'N' END,
        b.FIND_NUMBER,
        b.ITEM_NUMBER,
        b.QUANTITY,
        b.DESCRIPTION,
        rd_agg.REFDESIG_LABELS,
        le_group.ENTRYVALUE,
        le_pri.ENTRYVALUE,
        c_in.CHANGE_NUMBER,
        c_out.CHANGE_NUMBER,
        ci.ID
    FROM AGILE.ITEM i
    INNER JOIN AGILE.REV r ON r.ITEM = i.ID
    INNER JOIN AGILE.BOM b ON b.ITEM = i.ID
    LEFT JOIN AGILE.ITEM ci ON ci.ITEM_NUMBER = b.ITEM_NUMBER
    LEFT JOIN (
        SELECT rd.BOM, LISTAGG(rd.LABEL, ',') WITHIN GROUP (ORDER BY rd.LABEL) AS REFDESIG_LABELS
        FROM AGILE.REFDESIG rd GROUP BY rd.BOM
    ) rd_agg ON rd_agg.BOM = b.ID
    LEFT JOIN AGILE.LISTENTRY le_group ON le_group.ENTRYID = b.LIST06 AND le_group.LANGID = 4
    LEFT JOIN AGILE.LISTENTRY le_pri ON le_pri.ENTRYID = b.LIST07 AND le_pri.LANGID = 4
    LEFT JOIN AGILE.CHANGE c_in ON c_in.ID = b.CHANGE_IN
    LEFT JOIN AGILE.CHANGE c_out ON c_out.ID = b.CHANGE_OUT AND b.CHANGE_OUT != 0
    WHERE i.ITEM_NUMBER = '替换为物料编号'
      AND (b.CHANGE_IN IS NULL OR b.CHANGE_IN = 0 OR EXISTS (
              SELECT 1 FROM AGILE.REV r_in
              WHERE r_in.ITEM = i.ID AND r_in.CHANGE = b.CHANGE_IN
                AND r_in.RELEASE_DATE <= r.RELEASE_DATE))
      AND (b.CHANGE_OUT IS NULL OR b.CHANGE_OUT = 0 OR NOT EXISTS (
              SELECT 1 FROM AGILE.REV r_out
              WHERE r_out.ITEM = i.ID AND r_out.CHANGE = b.CHANGE_OUT
                AND r_out.RELEASE_DATE <= r.RELEASE_DATE))

    UNION ALL

    -- ===== 递归：子级物料使用最新版本展开（无窗口函数）=====
    SELECT
        bt.lvl + 1,
        bt.sort_path || '.' || LPAD(REGEXP_REPLACE(b2.FIND_NUMBER, '[^0-9]', ''), 4, '0'),
        bt.component_item,
        bt.component_desc,
        r2.REV_NUMBER,
        r2.RELEASE_DATE,
        'Y',
        b2.FIND_NUMBER,
        b2.ITEM_NUMBER,
        b2.QUANTITY,
        b2.DESCRIPTION,
        rd2.REFDESIG_LABELS,
        leg2.ENTRYVALUE,
        lep2.ENTRYVALUE,
        ci2.CHANGE_NUMBER,
        co2.CHANGE_NUMBER,
        ci2_id.ID
    FROM bom_tree bt
    INNER JOIN AGILE.ITEM i2 ON i2.ITEM_NUMBER = bt.component_item
    INNER JOIN AGILE.REV r2 ON r2.ITEM = i2.ID AND r2.LATEST_FLAG = 1
    INNER JOIN AGILE.BOM b2 ON b2.ITEM = i2.ID
    LEFT JOIN AGILE.ITEM ci2_id ON ci2_id.ITEM_NUMBER = b2.ITEM_NUMBER
    LEFT JOIN (
        SELECT rd.BOM, LISTAGG(rd.LABEL, ',') WITHIN GROUP (ORDER BY rd.LABEL) AS REFDESIG_LABELS
        FROM AGILE.REFDESIG rd GROUP BY rd.BOM
    ) rd2 ON rd2.BOM = b2.ID
    LEFT JOIN AGILE.LISTENTRY leg2 ON leg2.ENTRYID = b2.LIST06 AND leg2.LANGID = 4
    LEFT JOIN AGILE.LISTENTRY lep2 ON lep2.ENTRYID = b2.LIST07 AND lep2.LANGID = 4
    LEFT JOIN AGILE.CHANGE ci2 ON ci2.ID = b2.CHANGE_IN
    LEFT JOIN AGILE.CHANGE co2 ON co2.ID = b2.CHANGE_OUT AND b2.CHANGE_OUT != 0
    WHERE bt.lvl < 10
      AND bt.component_item_id IS NOT NULL
      AND (b2.CHANGE_IN IS NULL OR b2.CHANGE_IN = 0 OR EXISTS (
              SELECT 1 FROM AGILE.REV rx
              WHERE rx.ITEM = i2.ID AND rx.CHANGE = b2.CHANGE_IN
                AND rx.RELEASE_DATE <= r2.RELEASE_DATE))
      AND (b2.CHANGE_OUT IS NULL OR b2.CHANGE_OUT = 0 OR NOT EXISTS (
              SELECT 1 FROM AGILE.REV ry
              WHERE ry.ITEM = i2.ID AND ry.CHANGE = b2.CHANGE_OUT
                AND ry.RELEASE_DATE <= r2.RELEASE_DATE))
)
SELECT
    t.lvl                                   AS "层级",
    CASE WHEN t.rn = 1 THEN t.parent_item_number ELSE '' END  AS "父项物料编号",
    CASE WHEN t.rn = 1 THEN t.parent_desc ELSE '' END         AS "父项描述",
    CASE WHEN t.rn = 1 THEN t.rev_number ELSE '' END          AS "版本",
    CASE WHEN t.rn = 1 THEN TO_CHAR(t.release_date, 'YYYY-MM-DD') ELSE '' END AS "发布日期",
    CASE WHEN t.rn = 1 THEN t.is_latest ELSE '' END           AS "最新",
    LPAD(' ', (t.lvl - 1) * 4) || t.find_number AS "序号",
    t.component_item                        AS "子项物料编号",
    t.quantity                              AS "数量",
    t.component_desc                        AS "子项描述",
    t.refdesig_labels                       AS "贴片位号",
    t.sub_group                             AS "替代组别",
    t.sub_priority                          AS "替代优先级",
    t.change_in_number                      AS "加入Change",
    t.change_out_number                     AS "移除Change",
    CASE WHEN t.component_item_id IS NOT NULL THEN 'Y' ELSE 'N' END AS "有下级BOM"
FROM (
    SELECT bt.*,
           ROW_NUMBER() OVER (
               PARTITION BY bt.parent_item_number, bt.rev_number
               ORDER BY bt.sort_path) AS rn
    FROM bom_tree bt
) t
ORDER BY t.sort_path;
```

---

## 五、SQL：多级BOM树形展开（所有Item，仅最新版本）

> 输出数据库中所有存在BOM的Item，仅展开最新版本，递归到最低层级。
> 适合作为全量报表导出。兼容 Oracle 12.0+。
> 
> **修复说明**：原始版本在递归CTE内部使用了 `ROW_NUMBER() OVER(...)` 窗口函数，会触发
> `ORA-00600 [qkebCreateColInFro:1]` 内部错误（Oracle 12c 已知Bug）。
> 修复方案：新增 `bom_seq` CTE 在递归CTE之外预计算BOM行序号，
> 递归CTE中直接引用预计算结果 `bom_seq.item_row_seq`，完全避免在递归CTE内使用窗口函数。

```sql
WITH
-- ===== CTE 0: 预计算 BOM 行序号（仅保留必要列，避免 b.* 拖入60+冗余列）=====
bom_seq AS (
    SELECT
        b.ID, b.ITEM, b.ITEM_NUMBER, b.FIND_NUMBER, b.QUANTITY,
        b.DESCRIPTION, b.LIST06, b.LIST07,
        b.CHANGE_IN, b.CHANGE_OUT,
        ROW_NUMBER() OVER (
            PARTITION BY b.ITEM
            ORDER BY TO_NUMBER(REGEXP_REPLACE(b.FIND_NUMBER, '[^0-9]', ''))
        ) AS item_row_seq
    FROM AGILE.BOM b
),

-- ===== CTE 1: 预聚合贴片位号（锚点+递归共用，避免重复扫 REFDESIG）=====
refdesig_agg AS (
    SELECT
        rd.BOM,
        LISTAGG(rd.LABEL, ',') WITHIN GROUP (ORDER BY rd.LABEL) AS labels
    FROM AGILE.REFDESIG rd
    GROUP BY rd.BOM
),

-- ===== CTE 2: 标记哪些 Item 自身还有下级 BOM（避免逐行 EXISTS 子查询）=====
item_has_bom AS (
    SELECT DISTINCT b.ITEM AS item_id
    FROM AGILE.BOM b
),

-- ===== CTE 3: 预映射 (ITEM, CHANGE) → 首个发布日（替代逐行 EXISTS REV 子查询）=====
rev_change_date AS (
    SELECT
        r.ITEM  AS item_id,
        r.CHANGE AS change_id,
        r.RELEASE_DATE
    FROM AGILE.REV r
    WHERE r.RELEASE_DATE IS NOT NULL
),

bom_tree (
    lvl, sort_path,
    parent_item_number, parent_desc,
    rev_number, release_date,
    find_number, component_item, quantity, component_desc,
    refdesig_labels, sub_group, sub_priority,
    change_in_number, change_out_number,
    has_child_bom
) AS (
    -- ===== 锚点：所有有 BOM 的 Item，仅最新版本 =====
    SELECT
        1,
        i.ITEM_NUMBER || '.' || LPAD(TO_CHAR(b.item_row_seq), 4, '0'),
        i.ITEM_NUMBER,
        i.DESCRIPTION,
        r.REV_NUMBER,
        r.RELEASE_DATE,
        b.FIND_NUMBER,
        b.ITEM_NUMBER,
        b.QUANTITY,
        b.DESCRIPTION,
        rd.labels,
        le_group.ENTRYVALUE,
        le_pri.ENTRYVALUE,
        c_in.CHANGE_NUMBER,
        c_out.CHANGE_NUMBER,
        CASE WHEN hb.item_id IS NOT NULL THEN 1 ELSE 0 END
    FROM AGILE.ITEM i
    INNER JOIN AGILE.REV r ON r.ITEM = i.ID AND r.LATEST_FLAG = 1
    INNER JOIN bom_seq b ON b.ITEM = i.ID
    LEFT JOIN AGILE.ITEM ci ON ci.ITEM_NUMBER = b.ITEM_NUMBER
    LEFT JOIN refdesig_agg rd ON rd.BOM = b.ID
    LEFT JOIN item_has_bom hb ON hb.item_id = ci.ID
    LEFT JOIN AGILE.LISTENTRY le_group ON le_group.ENTRYID = b.LIST06 AND le_group.LANGID = 4
    LEFT JOIN AGILE.LISTENTRY le_pri  ON le_pri.ENTRYID  = b.LIST07 AND le_pri.LANGID = 4
    LEFT JOIN AGILE.CHANGE c_in  ON c_in.ID  = b.CHANGE_IN
    LEFT JOIN AGILE.CHANGE c_out ON c_out.ID = b.CHANGE_OUT AND b.CHANGE_OUT != 0
    LEFT JOIN rev_change_date rcd_in
        ON rcd_in.item_id = i.ID AND rcd_in.change_id = b.CHANGE_IN
    LEFT JOIN rev_change_date rcd_out
        ON rcd_out.item_id = i.ID AND rcd_out.change_id = b.CHANGE_OUT
    WHERE (b.CHANGE_IN IS NULL OR b.CHANGE_IN = 0
           OR rcd_in.RELEASE_DATE <= r.RELEASE_DATE)
      AND (b.CHANGE_OUT IS NULL OR b.CHANGE_OUT = 0
           OR rcd_out.RELEASE_DATE IS NULL
           OR rcd_out.RELEASE_DATE > r.RELEASE_DATE)

    UNION ALL

    -- ===== 递归：子级物料最新版本展开 =====
    SELECT
        bt.lvl + 1,
        bt.sort_path || '.' || LPAD(TO_CHAR(b2.item_row_seq), 4, '0'),
        bt.component_item,
        bt.component_desc,
        r2.REV_NUMBER,
        r2.RELEASE_DATE,
        b2.FIND_NUMBER,
        b2.ITEM_NUMBER,
        b2.QUANTITY,
        b2.DESCRIPTION,
        rd2.labels,
        leg2.ENTRYVALUE,
        lep2.ENTRYVALUE,
        ci2.CHANGE_NUMBER,
        co2.CHANGE_NUMBER,
        CASE WHEN hb2.item_id IS NOT NULL THEN 1 ELSE 0 END
    FROM bom_tree bt
    INNER JOIN AGILE.ITEM i2 ON i2.ITEM_NUMBER = bt.component_item
    INNER JOIN AGILE.REV r2 ON r2.ITEM = i2.ID AND r2.LATEST_FLAG = 1
    INNER JOIN bom_seq b2 ON b2.ITEM = i2.ID
    LEFT JOIN AGILE.ITEM ci2_id ON ci2_id.ITEM_NUMBER = b2.ITEM_NUMBER
    LEFT JOIN refdesig_agg rd2 ON rd2.BOM = b2.ID
    LEFT JOIN item_has_bom hb2 ON hb2.item_id = ci2_id.ID
    LEFT JOIN AGILE.LISTENTRY leg2 ON leg2.ENTRYID = b2.LIST06 AND leg2.LANGID = 4
    LEFT JOIN AGILE.LISTENTRY lep2 ON lep2.ENTRYID = b2.LIST07 AND lep2.LANGID = 4
    LEFT JOIN AGILE.CHANGE ci2 ON ci2.ID = b2.CHANGE_IN
    LEFT JOIN AGILE.CHANGE co2 ON co2.ID = b2.CHANGE_OUT AND b2.CHANGE_OUT != 0
    LEFT JOIN rev_change_date rcd_in2
        ON rcd_in2.item_id = i2.ID AND rcd_in2.change_id = b2.CHANGE_IN
    LEFT JOIN rev_change_date rcd_out2
        ON rcd_out2.item_id = i2.ID AND rcd_out2.change_id = b2.CHANGE_OUT
    WHERE bt.lvl < 10
      AND bt.has_child_bom = 1
      AND (b2.CHANGE_IN IS NULL OR b2.CHANGE_IN = 0
           OR rcd_in2.RELEASE_DATE <= r2.RELEASE_DATE)
      AND (b2.CHANGE_OUT IS NULL OR b2.CHANGE_OUT = 0
           OR rcd_out2.RELEASE_DATE IS NULL
           OR rcd_out2.RELEASE_DATE > r2.RELEASE_DATE)
)
SELECT
    bt.lvl                                    AS "层级",
    CASE WHEN bt.rn = 1 THEN bt.parent_item_number END AS "父项物料编号",
    CASE WHEN bt.rn = 1 THEN bt.parent_desc      END AS "父项描述",
    CASE WHEN bt.rn = 1 THEN bt.rev_number       END AS "版本",
    CASE WHEN bt.rn = 1
         THEN TO_CHAR(bt.release_date, 'YYYY-MM-DD') END AS "发布日期",
    LPAD(' ', (bt.lvl - 1) * 4) || bt.find_number AS "序号",
    bt.component_item                         AS "子项物料编号",
    bt.quantity                               AS "数量",
    bt.component_desc                         AS "子项描述",
    bt.refdesig_labels                        AS "贴片位号",
    NVL(bt.sub_group,    '-')                 AS "替代组别",
    NVL(bt.sub_priority, '-')                 AS "替代优先级",
    NVL(bt.change_in_number,  '-')            AS "加入Change",
    NVL(bt.change_out_number, '-')            AS "移除Change",
    CASE WHEN bt.has_child_bom = 1 THEN 'Y' ELSE 'N' END AS "有下级BOM"
FROM (
    SELECT bt2.*,
           ROW_NUMBER() OVER (
               PARTITION BY bt2.parent_item_number, bt2.rev_number
               ORDER BY bt2.sort_path) AS rn
    FROM bom_tree bt2
) bt
ORDER BY bt.sort_path;
```

---

## 六、输出示例说明

上述SQL输出效果类似：

```
层级 | 父项物料编号    | 父项描述     | 版本 | 发布日期   | 序号     | 子项物料编号      | 数量 | 子项描述       | 贴片位号       | 替代组别 | 替代优先级 | 有下级BOM
-----|----------------|-------------|------|-----------|---------|------------------|------|---------------|---------------|---------|-----------|--------
1    | 10141-0L3X-0100| 主板组件     | R3   | 2026-07-01| 10      | 20001-AB-001     | 1    | 电源模块       | U1,U2         | GroupA  | 1         | Y
2    |                |             |      |           |   10    | 30001-CD-002     | 2    | 电容100nF     | C1,C2,C3      | -       | -         | N
2    |                |             |      |           |   20    | 30002-EF-003     | 1    | 电阻10K       | R1,R2         | GroupB  | 1         | N
2    |                |             |      |           |   30    | 30003-GH-004     | 1    | IC芯片        | U3            | -       | -         | N
1    |                |             |      |           | 20      | 20002-IJ-005     | 1    | 连接器         | J1            | -       | -         | N
1    |                |             |      |           | 30      | 20003-KL-006     | 4    | LED           | D1,D2,D3,D4   | GroupC  | 2         | N
```

- **层级1**：顶层Item的直接BOM子项
- **层级2+**：子项自身BOM的递归展开（使用子项的最新版本）
- **父项列**：同一父项+版本组合只在首行显示，后续留空
- **序号缩进**：层级越深缩进越多，直观体现树形结构
- **有下级BOM**：标记该子项是否还有下级展开（前端可据此显示展开箭头）
