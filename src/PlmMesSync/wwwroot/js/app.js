const API_BASE = '';
const REFRESH_INTERVAL = 3000;

let currentTab = 'overview';
let logFiles = [];
let cachedTriggers = null;
let cachedSyncs = null;
let actPageIdx = 0;
const actPageSize = 100;
let actRows = [];

document.addEventListener('DOMContentLoaded', () => {
    initNav();
    initSidebar();
    startPolling();
    loadLogs();
});

function initSidebar() {
    const menuBtn = document.getElementById('btnMenu');
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebarOverlay');
    if (!menuBtn || !sidebar || !overlay) return;

    function openSidebar() {
        sidebar.classList.add('open');
        overlay.classList.add('active');
        document.body.style.overflow = 'hidden';
    }
    function closeSidebar() {
        sidebar.classList.remove('open');
        overlay.classList.remove('active');
        document.body.style.overflow = '';
    }
    menuBtn.addEventListener('click', openSidebar);
    overlay.addEventListener('click', closeSidebar);
    document.querySelectorAll('#sidebar .nav-item').forEach(item => {
        item.addEventListener('click', closeSidebar);
    });
}

function initNav() {
    document.querySelectorAll('.nav-item').forEach(item => {
        item.addEventListener('click', () => switchTab(item.dataset.tab));
    });

    document.querySelectorAll('[data-goto]').forEach(btn => {
        btn.addEventListener('click', () => switchTab(btn.dataset.goto));
    });

    document.getElementById('refreshLogs').addEventListener('click', loadLogs);
    document.getElementById('refreshFileLogs').addEventListener('click', loadFileLogs);
    document.getElementById('btnRefresh').addEventListener('click', () => {
        fetchStatus();
        fetchTriggers();
        fetchSyncs();
        fetchFileSyncStatus();
        fetchFileRequests();
        if (currentTab === 'logs') loadLogs();
        if (currentTab === 'filelogs') loadFileLogs();
    });
    document.addEventListener('keydown', e => { if (e.key === 'Escape') closeFileDetail(); });
}

function switchTab(tab) {
    currentTab = tab;
    document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
    document.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));
    document.querySelector(`[data-tab="${tab}"]`).classList.add('active');
    document.getElementById(`tab-${tab}`).classList.add('active');

    const titles = { overview: 'Overview', triggers: 'Trigger Records', syncs: 'Sync Records', logs: 'Log Files', filerequests: 'File Requests', filelogs: 'File Logs' };
    document.getElementById('pageTitle').textContent = titles[tab] || tab;

    if (tab === 'logs') loadLogs();
    if (tab === 'filelogs') loadFileLogs();
}

function startPolling() {
    fetchStatus();
    fetchTriggers();
    fetchSyncs();
    fetchFileSyncStatus();
    fetchFileRequests();
    setInterval(() => {
        fetchStatus();
        fetchTriggers();
        fetchSyncs();
    }, REFRESH_INTERVAL);
    setInterval(() => {
        fetchFileSyncStatus();
        fetchFileRequests();
    }, 5000);
}

async function fetchStatus() {
    try {
        const res = await fetch(`${API_BASE}/api/dashboard/status`);
        const data = await res.json();
        document.getElementById('statusDot').className = 'conn-dot online';
        document.getElementById('statusText').textContent = 'Running';
        document.getElementById('statTriggers').textContent = data.totalTriggers;
        document.getElementById('statSyncs').textContent = data.totalSyncs;
        document.getElementById('statPending').textContent = data.pendingSyncs;
        document.getElementById('navTriggerCount').textContent = data.totalTriggers;
        document.getElementById('navSyncCount').textContent = data.totalSyncs;
        document.getElementById('uptime').textContent = `Uptime: ${data.uptime}`;
        document.getElementById('lastRefresh').textContent = new Date().toLocaleTimeString();
    } catch {
        document.getElementById('statusDot').className = 'conn-dot offline';
        document.getElementById('statusText').textContent = 'Offline';
    }
}

let _fetchPending = 0;

async function fetchTriggers() {
    _fetchPending++;
    try {
        const res = await fetch(`${API_BASE}/api/dashboard/triggers?count=200`);
        const data = await res.json();
        cachedTriggers = data;
        renderTriggers(data);
    } catch { /* ignore */ }
    _fetchPending--;
    if (_fetchPending === 0) renderActivity();
}

async function fetchSyncs() {
    _fetchPending++;
    try {
        const res = await fetch(`${API_BASE}/api/dashboard/syncs?count=200`);
        const data = await res.json();
        cachedSyncs = data;
        renderSyncs(data);
        updateMesStats(data);
    } catch { /* ignore */ }
    _fetchPending--;
    if (_fetchPending === 0) renderActivity();
}

async function loadLogs() {
    try {
        const res = await fetch(`${API_BASE}/api/dashboard/logs`);
        logFiles = await res.json();
        renderLogsList();
    } catch {
        const container = document.getElementById('logsList');
        container.innerHTML = '<p class="empty-hint">Failed to load logs</p>';
    }
}

function renderTriggers(data) {
    const tbody = document.getElementById('triggersBody');
    if (!data || data.length === 0) {
        tbody.innerHTML = '<tr class="empty-row"><td colspan="6">Waiting for triggers...</td></tr>';
        return;
    }

    tbody.innerHTML = data.map(r => `
        <tr>
            <td>${r.sequence}</td>
            <td><span class="badge badge-type-${String(r.table).toLowerCase()}">${r.table}</span></td>
            <td><strong>${r.bomId}</strong></td>
            <td><span class="badge badge-${String(r.action).toLowerCase()}">${r.action}</span></td>
            <td>${r.table}</td>
            <td>${formatTime(r.receivedAt)}</td>
        </tr>
    `).join('');
}

function updateMesStats(syncs) {
    if (!syncs) return;
    var ok = 0, fail = 0, fileOk = 0, fileFail = 0;
    for (var i = 0; i < syncs.length; i++) {
        var s = syncs[i];
        if (s.table === 'FILES') {
            if (s.status === 'SUCCESS') fileOk++;
            else if (s.status === 'FAILED') fileFail++;
        } else {
            if (s.mesStatus === 'OK') ok++;
            else if (s.mesStatus && s.mesStatus.startsWith('FAILED')) fail++;
        }
    }
    document.getElementById('statMesOk').textContent = ok;
    document.getElementById('statMesFail').textContent = fail;
    document.getElementById('statFileOk').textContent = fileOk;
    document.getElementById('statFileFail').textContent = fileFail;
}

function renderActivity() {
    var tbody = document.getElementById('overviewActivity');
    if (!tbody) return;

    actRows = [];
    var triggerByBom = {};

    if (cachedTriggers) {
        for (var i = 0; i < cachedTriggers.length; i++) {
            var t = cachedTriggers[i];
            var key = '' + t.bomId;
            if (!triggerByBom[key]) triggerByBom[key] = [];
            triggerByBom[key].push(t);
        }
    }

    if (cachedSyncs) {
        for (var i = 0; i < cachedSyncs.length; i++) {
            var s = cachedSyncs[i];
            var key = '' + s.bomId;
            var trig = null;
            var arr = triggerByBom[key];
            if (arr && arr.length > 0) {
                for (var j = 0; j < arr.length; j++) {
                    if (!arr[j]._used) {
                        trig = arr[j];
                        arr[j]._used = true;
                        break;
                    }
                }
            }
            var item = s.itemNumber || (s.table === 'FILES' ? 'FILE #' + s.bomId : 'BOM #' + s.bomId);
            actRows.push({
                time: s.executedAt,
                item: item,
                bomId: s.bomId,
                table: s.table || 'BOM',
                trigger: trig ? { status: trig.action } : null,
                build: { status: s.status },
                sync: s.mesStatus ? { status: s.mesStatus === 'OK' ? 'OK' : (s.mesStatus.startsWith('FAILED') ? 'FAIL' : (s.mesStatus.startsWith('FileSync') ? 'OK' : s.mesStatus)) } : null,
                logFile: s.logFile || null
            });
        }
    }

    for (var key in triggerByBom) {
        var arr = triggerByBom[key];
        for (var i = 0; i < arr.length; i++) {
            if (!arr[i]._used) {
                actRows.push({
                    time: arr[i].receivedAt,
                    item: (arr[i].table === 'FILES' ? 'FILE #' : 'BOM #') + arr[i].bomId,
                    bomId: arr[i].bomId,
                    table: arr[i].table || 'BOM',
                    trigger: { status: arr[i].action },
                    build: null,
                    sync: null,
                    logFile: null
                });
            }
        }
    }

    actRows.sort(function (a, b) { return new Date(b.time) - new Date(a.time); });

    document.getElementById('actCount').textContent = actRows.length + ' records';

    if (actRows.length === 0) {
        tbody.innerHTML = '<tr class="empty-row"><td colspan="6">Waiting...</td></tr>';
        document.getElementById('actPager').style.display = 'none';
        return;
    }

    actPageIdx = 0;
    renderActPage();
}

function renderActPage() {
    var tbody = document.getElementById('overviewActivity');
    var start = actPageIdx * actPageSize;
    var end = Math.min(start + actPageSize, actRows.length);
    var page = actRows.slice(start, end);

    var totalPages = Math.ceil(actRows.length / actPageSize);
    document.getElementById('actPageInfo').textContent = (actPageIdx + 1) + ' / ' + totalPages;
    document.getElementById('actPager').style.display = 'flex';
    document.getElementById('actPrev').style.visibility = actPageIdx > 0 ? 'visible' : 'hidden';
    document.getElementById('actNext').style.visibility = actPageIdx < totalPages - 1 ? 'visible' : 'hidden';

    function badge(v, clsOk, clsFail) {
        if (!v) return '<span class="badge badge-muted">-</span>';
        if (v.status === 'OK' || v.status === 'SUCCESS' || v.status === 'INSERT' || v.status === 'UPDATE') {
            return '<span class="badge ' + clsOk + '">' + escapeHtml(v.status) + '</span>';
        }
        return '<span class="badge ' + clsFail + '">' + escapeHtml(v.status) + '</span>';
    }

    tbody.innerHTML = page.map(function (r) {
        var hasErr = (r.build && (r.build.status === 'FAILED' || r.build.status === 'SKIPPED'))
                  || (r.sync && r.sync.status === 'FAIL');
        var allOk = r.build && r.build.status === 'SUCCESS' && (r.sync ? r.sync.status === 'OK' || r.sync.status.startsWith('FileSync') : true);
        var cls = 'act-row';
        if (hasErr) cls += ' act-err';
        else if (allOk) cls += ' act-ok';
        else if (r.build && r.sync) cls += ' act-part';

        var typeBadge = '<span class="badge badge-type-' + String(r.table).toLowerCase() + '">' + escapeHtml(r.table) + '</span>';
        var clickable = r.logFile ? ' style="cursor:pointer" onclick="viewLog(\'' + escapeHtml(r.logFile.split(/[/\\]/).pop()) + '\')"' : '';
        return '<tr class="' + cls + '"' + clickable + '>'
            + '<td class="act-time">' + formatTime(r.time) + '</td>'
            + '<td>' + typeBadge + '</td>'
            + '<td class="act-detail">' + escapeHtml(r.item) + '</td>'
            + '<td>' + badge(r.trigger, 'badge-insert', 'badge-delete') + '</td>'
            + '<td>' + badge(r.build, 'badge-success', 'badge-failed') + '</td>'
            + '<td>' + badge(r.sync, 'badge-success', 'badge-failed') + '</td>'
            + '</tr>';
    }).join('');
}

function actPage(dir) {
    var totalPages = Math.ceil(actRows.length / actPageSize);
    var next = actPageIdx + dir;
    if (next < 0 || next >= totalPages) return;
    actPageIdx = next;
    renderActPage();
}

function renderSyncs(data) {
    const tbody = document.getElementById('syncsBody');
    if (!data || data.length === 0) {
        tbody.innerHTML = '<tr class="empty-row"><td colspan="9">No sync records yet...</td></tr>';
        return;
    }

    tbody.innerHTML = data.map(r => {
        let logCell = '-';
        if (r.logFile) {
            const fileName = r.logFile.split(/[/\\]/).pop();
            logCell = `<a class="log-link" onclick="viewLog('${fileName}')">View Log</a>`;
        } else if (r.error) {
            logCell = `<span class="error-text" title="${escapeHtml(r.error)}">${escapeHtml(r.error)}</span>`;
        }

        let mesCell = '-';
        if (r.table === 'FILES') {
            if (r.status === 'SUCCESS') {
                mesCell = '<span class="badge badge-success">SYNCED</span>';
            } else if (r.status === 'FAILED') {
                mesCell = '<span class="badge badge-failed">FAIL</span>';
            }
        } else if (r.mesStatus === 'OK') {
            mesCell = '<span class="badge badge-success">OK</span>';
        } else if (r.mesStatus && r.mesStatus.startsWith('FAILED')) {
            mesCell = `<span class="badge badge-failed" title="${escapeHtml(r.mesStatus)}">FAIL</span>`;
        }

        const typeBadge = `<span class="badge badge-type-${String(r.table || 'BOM').toLowerCase()}">${r.table || 'BOM'}</span>`;

        return `
            <tr>
                <td>${r.sequence}</td>
                <td>${typeBadge}</td>
                <td><strong>${r.bomId}</strong></td>
                <td>${r.itemNumber || '-'}</td>
                <td><span class="badge badge-${String(r.status).toLowerCase()}">${r.status}</span></td>
                <td>${mesCell}</td>
                <td>${formatTime(r.triggeredAt)}</td>
                <td>${formatTime(r.executedAt)}</td>
                <td>${logCell}</td>
            </tr>
        `;
    }).join('');
}

function renderLogsList() {
    const container = document.getElementById('logsList');
    if (!logFiles || logFiles.length === 0) {
        container.innerHTML = '<p class="empty-hint">No log files yet</p>';
        return;
    }

    container.innerHTML = logFiles.map(f =>
        `<div class="log-file-item" title="${f}" onclick="viewLog('${f}')">${f}</div>`
    ).join('');
}

async function viewLog(filename) {
    try {
        const res = await fetch(`${API_BASE}/api/dashboard/logs/${encodeURIComponent(filename)}`);
        if (!res.ok) throw new Error('Not found');
        const content = await res.text();
        document.getElementById('viewerFileName').textContent = filename;

        const mesIdx = content.indexOf('MES Upload Log');
        const viewerEl = document.getElementById('viewerContent');
        const mesSection = document.getElementById('mesSection');

        if (mesIdx === -1) {
            viewerEl.innerHTML = parseBomReport(content);
            mesSection.style.display = 'none';
        } else {
            viewerEl.innerHTML = parseBomReport(content.substring(0, mesIdx));
            mesSection.innerHTML = parseMesSection(content.substring(mesIdx));
            mesSection.style.display = 'block';
        }

        document.querySelectorAll('.log-file-item').forEach(el => {
            el.classList.toggle('active', el.textContent.trim() === filename);
        });

        if (currentTab !== 'logs') switchTab('logs');
    } catch {
        document.getElementById('viewerContent').textContent = 'Failed to load log file.';
        document.getElementById('mesSection').style.display = 'none';
    }
}

function parseBomReport(text) {
    const lines = text.split('\n');
    const parts = [];
    const info = { parent: '', desc: '', generated: '', totalLines: 0 };
    let current = null;

    for (const line of lines) {
        const t = line.trim();

        if (t.startsWith('Parent Item:')) info.parent = t.replace('Parent Item:', '').trim();
        else if (t.startsWith('Description:')) info.desc = t.replace('Description:', '').trim();
        else if (t.startsWith('Generated:')) info.generated = t.replace('Generated:', '').trim();
        else if (t.startsWith('End of Report')) {
            const m = t.match(/lines:\s*(\d+)/);
            if (m) info.totalLines = parseInt(m[1]);
        }

        if (line.startsWith('--- Version:')) {
            if (current) parts.push(current);
            current = { title: t.replace(/^---\s*/, '').replace(/\s*---$/, ''), rows: [] };
            continue;
        }

        if (current && t.includes('|') && !t.includes('Lvl') && !t.startsWith('(') && !t.match(/^[- ]+$/)) {
            current.rows.push(line);
        }
    }
    if (current) parts.push(current);

    if (parts.length === 0) return escapeHtml(text);

    let html = '';
    if (info.parent || info.desc) {
        html += `<div class="bom-head">`;
        if (info.parent) html += `<div class="bom-head-item">${escapeHtml(info.parent)}</div>`;
        if (info.desc) html += `<div class="bom-head-desc">${escapeHtml(info.desc)}</div>`;
        if (info.generated) html += `<div class="bom-head-meta">${escapeHtml(info.generated)}</div>`;
        html += `</div>`;
    }

    for (const part of parts) {
        html += `<div class="bom-version">`;
        html += `<div class="bom-version-title">${escapeHtml(part.title)}</div>`;

        if (part.rows.length === 0) {
            html += `<div class="bom-empty">(No active BOM lines)</div>`;
        } else {
            const colLabels = ['Find#', 'Item Number', 'Qty', 'Rev', 'Description', 'RefDesig', 'SubGrp', 'SubPri', 'ChgIn', 'ChgOut'];
            html += `<table class="bom-table"><thead><tr>${colLabels.map(l => `<th>${l}</th>`).join('')}</tr></thead><tbody>`;

            for (const row of part.rows) {
                const cells = row.split('|').map(c => c.trim());
                const first = cells[0] || '';
                const m = first.match(/\d+\+?\s+(.+)/);
                html += '<tr>';
                html += `<td class="bom-fn">${escapeHtml(m ? m[1].trim() : first)}</td>`;
                for (let i = 1; i < cells.length; i++) {
                    html += `<td>${escapeHtml(cells[i])}</td>`;
                }
                for (let i = cells.length; i < 10; i++) html += '<td>-</td>';
                html += '</tr>';
            }
            html += '</tbody></table>';
        }
        html += '</div>';
    }

    return html;
}

function parseMesSection(text) {
    const lines = text.split('\n');
    let endpoint = '', duration = '', httpStatus = '';
    let requestTime = '', responseTime = '';
    let mode = 'header';
    const reqLines = [], resLines = [];

    for (const line of lines) {
        const t = line.trim();

        if (t.startsWith('Endpoint:')) { endpoint = t.replace('Endpoint:', '').trim(); continue; }
        if (t.startsWith('Request Time:')) { requestTime = t.replace('Request Time:', '').trim(); continue; }
        if (t.startsWith('Response Time:')) { responseTime = t.replace('Response Time:', '').trim(); continue; }
        if (t.startsWith('Duration:')) { duration = t.replace('Duration:', '').trim(); continue; }
        if (t.startsWith('HTTP Status:')) { httpStatus = t.replace('HTTP Status:', '').trim(); continue; }

        if (t.startsWith('--- Request Body')) { mode = 'request'; continue; }
        if (t.startsWith('--- Response Body')) { mode = 'response'; continue; }
        if (t.startsWith('===')) { if (mode === 'response') mode = 'done'; continue; }
        if (mode === 'request') reqLines.push(line);
        else if (mode === 'response') resLines.push(line);
    }

    const isFail = httpStatus === 'N/A (exception)' || httpStatus.startsWith('4') || httpStatus.startsWith('5');
    const isOk = !isFail && httpStatus !== '';
    const blocCls = isOk ? 'mes-ok' : 'mes-fail';
    const label = isOk ? 'Upload Successful' : 'Upload Failed';
    const reqBody = reqLines.join('\n').trim();
    const resBody = resLines.join('\n').trim();
    const metaParts = [
        endpoint && `Endpoint: <code>${escapeHtml(endpoint)}</code>`,
        requestTime && `Sent: ${escapeHtml(requestTime)}`,
        responseTime && `Received: ${escapeHtml(responseTime)}`,
    ].filter(Boolean);

    return `
        <div class="mes-block ${blocCls}">
            <div class="mes-block-bar">
                <span class="mes-block-dot"></span>
                <span class="mes-block-label">${label}</span>
                <span class="mes-block-stat">${escapeHtml(duration)} · HTTP ${escapeHtml(httpStatus)}</span>
            </div>
            ${metaParts.length ? `<div class="mes-block-meta">${metaParts.join('<br>')}</div>` : ''}
            <div class="mes-block-section">
                <div class="mes-block-section-hd" onclick="toggleMes(this)">
                    <span class="arrow"></span>
                    Request Body
                    <span class="sz">${fmtSize(reqBody)}</span>
                </div>
                <pre class="mes-block-section-body collapsed">${escapeHtml(reqBody || '(empty)')}</pre>
            </div>
            <div class="mes-block-section">
                <div class="mes-block-section-hd" onclick="toggleMes(this)">
                    <span class="arrow"></span>
                    Response Body
                    <span class="sz">${fmtSize(resBody)}</span>
                </div>
                <pre class="mes-block-section-body collapsed">${escapeHtml(resBody || '(empty)')}</pre>
            </div>
        </div>`;
}

function toggleMes(header) {
    const body = header.nextElementSibling;
    const arrow = header.querySelector('.arrow');
    body.classList.toggle('collapsed');
    arrow.classList.toggle('open');
}

function fmtSize(s) {
    if (!s) return 'empty';
    const b = s.length;
    if (b < 1000) return b + 'B';
    return (b / 1000).toFixed(1) + 'KB';
}

function formatTime(isoString) {
    if (!isoString) return '-';
    const d = new Date(isoString);
    const now = new Date();
    const diff = (now - d) / 1000;

    if (diff < 60) return `${Math.floor(diff)}s ago`;
    if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;

    return d.toLocaleString('zh-CN', {
        month: '2-digit', day: '2-digit',
        hour: '2-digit', minute: '2-digit', second: '2-digit'
    });
}

function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

/* ===== FileSyncService Proxy Integration ===== */

async function fetchFileSyncStatus() {
    try {
        const res = await fetch(`${API_BASE}/api/dashboard/filesync/status`);
        const body = await res.json();
        const d = body.data || body;
        document.getElementById('navFileReqCount').textContent = d.totalRequests || 0;
    } catch { /* FileSyncService offline */ }
}

async function fetchFileRequests() {
    try {
        const res = await fetch(`${API_BASE}/api/dashboard/filesync/requests?count=200`);
        const body = await res.json();
        if (body.success && body.data) {
            renderFileRequestsTable(body.data);
        }
    } catch { /* ignore */ }
}

function renderFileRequestsTable(records) {
    const tbody = document.getElementById('fileRequestsBody');
    if (!records || records.length === 0) {
        tbody.innerHTML = '<tr class="empty-row"><td colspan="8">No requests yet...</td></tr>';
        return;
    }
    tbody.innerHTML = records.map(r => `
        <tr>
            <td><span style="font-family:var(--font-mono);color:var(--accent);font-size:0.78rem">${escapeHtml((r.requestId || '').substring(0, 8))}</span></td>
            <td>${formatTime(r.receivedTimeUtc)}</td>
            <td>${escapeHtml(r.clientIp || '-')}</td>
            <td>${r.totalFiles}</td>
            <td><span style="color:var(--green)">${r.successCount}</span> / <span style="color:var(--red)">${r.failedCount}</span></td>
            <td>${r.durationMs != null ? r.durationMs + 'ms' : '-'}</td>
            <td>${fileSyncStatusBadge(r.status)}</td>
            <td><button class="btn-sm" onclick="showFileDetail('${escapeHtml(r.requestId)}')">View</button></td>
        </tr>
    `).join('');
}

function fileSyncStatusBadge(status) {
    const map = { 'Success': 'badge-success', 'Failed': 'badge-failed', 'Processing': 'badge-processing', 'PartialSuccess': 'badge-partial' };
    return `<span class="badge ${map[status] || 'badge-muted'}">${escapeHtml(status || '-')}</span>`;
}

async function showFileDetail(requestId) {
    try {
        const res = await fetch(`${API_BASE}/api/dashboard/filesync/requests/${encodeURIComponent(requestId)}`);
        if (!res.ok) throw new Error('Not found');
        const body = await res.json();
        if (!body.success || !body.data) throw new Error('Invalid data');
        const r = body.data;

        document.getElementById('fileDetailMetaGrid').innerHTML = `
            <div class="detail-meta-item"><div class="meta-label">Request ID</div><div class="meta-value" style="font-family:var(--font-mono);font-size:0.78rem;color:var(--accent)">${escapeHtml(r.requestId)}</div></div>
            <div class="detail-meta-item"><div class="meta-label">Received</div><div class="meta-value">${formatTime(r.receivedTimeUtc)}</div></div>
            <div class="detail-meta-item"><div class="meta-label">Completed</div><div class="meta-value">${r.completedTimeUtc ? formatTime(r.completedTimeUtc) : '-'}</div></div>
            <div class="detail-meta-item"><div class="meta-label">Duration</div><div class="meta-value">${r.durationMs != null ? r.durationMs + ' ms' : '-'}</div></div>
            <div class="detail-meta-item"><div class="meta-label">Client IP</div><div class="meta-value">${escapeHtml(r.clientIp || '-')}</div></div>
            <div class="detail-meta-item"><div class="meta-label">Status</div><div class="meta-value">${fileSyncStatusBadge(r.status)}</div></div>
            <div class="detail-meta-item"><div class="meta-label">Files (OK/FAIL)</div><div class="meta-value"><span style="color:var(--green)">${r.successCount}</span> / <span style="color:var(--red)">${r.failedCount}</span></div></div>
            <div class="detail-meta-item"><div class="meta-label">Error</div><div class="meta-value" style="color:var(--red);font-size:0.78rem">${escapeHtml(r.errorMessage || '-')}</div></div>
        `;

        const filesEl = document.getElementById('fileDetailFiles');
        if (!r.results || r.results.length === 0) {
            filesEl.innerHTML = '<div class="file-card error"><div class="file-error">No file results available.</div></div>';
        } else {
            filesEl.innerHTML = r.results.map(f => renderFileSyncFileCard(f)).join('');
        }

        document.getElementById('fileDetailOverlay').classList.add('active');
    } catch {
        document.getElementById('fileDetailMetaGrid').innerHTML = '';
        document.getElementById('fileDetailFiles').innerHTML = '<div class="file-card error"><div class="file-error">Failed to load request details.</div></div>';
        document.getElementById('fileDetailOverlay').classList.add('active');
    }
}

function closeFileDetail(e) {
    if (e && e.target !== e.currentTarget) return;
    document.getElementById('fileDetailOverlay').classList.remove('active');
}

function renderFileSyncFileCard(f) {
    const cls = f.success ? '' : ' error';
    const statusCls = f.success ? 'badge-success' : 'badge-failed';
    const statusText = f.success ? 'Success' : 'Failed';

    let html = `<div class="file-card${cls}">
        <div class="file-card-header">
            <span class="file-card-name">${escapeHtml(f.fileName || 'Unknown')}</span>
            <span class="badge ${statusCls}">${statusText}</span>
        </div>
        <div class="file-card-grid">
            <span><span class="label">Inventory:</span> ${escapeHtml(f.inventoryCode || '-')}</span>
            <span><span class="label">FileCode:</span> ${escapeHtml(f.fileCode || '-')}</span>
            <span><span class="label">Size:</span> ${f.sizeBytes ? formatFileSize(f.sizeBytes) : '-'}</span>
            <span><span class="label">MD5:</span> <span style="font-family:var(--font-mono);font-size:0.72rem">${f.md5 ? f.md5.substring(0, 16) + '...' : '-'}</span></span>
            <span><span class="label">Path:</span> ${escapeHtml(f.relativePath || f.fullPath || '-')}</span>
            <span><span class="label">Archive:</span> ${f.isArchiveFile ? escapeHtml(f.archiveType || 'YES') : 'NO'}</span>
        </div>
        ${f.errorMessage ? `<div class="file-error">${escapeHtml(f.errorMessage)}</div>` : ''}`;

    if (f.steps && f.steps.length > 0) {
        const firstTime = new Date(f.steps[0].timestampUtc).getTime();
        html += `<div class="step-section"><div class="step-section-title">Processing Timeline (${f.steps.length} steps)</div><div class="step-grid">
            <span class="step-header">Step</span><span class="step-header">Detail</span><span class="step-header">Elapsed</span>`;
        for (const s of f.steps) {
            const elapsed = new Date(s.timestampUtc).getTime() - firstTime;
            const stepCls = (s.step || '').match(/ERROR|TIMEOUT|SECURITY|CANCELLED/) ? 'step-error'
                : (s.step || '').match(/SUCCESS|COMPLETE|FOUND/) ? 'step-ok' : '';
            html += `<span class="step-cell ${stepCls}">${escapeHtml(s.step || '')}</span>
                <span class="step-cell step-detail">${escapeHtml(truncateStr(s.detail, 60))}</span>
                <span class="step-cell step-elapsed">+${elapsed}ms</span>`;
        }
        html += '</div></div>';
    }

    if ((f.isZipFile || f.isArchiveFile) && f.zipEntries && f.zipEntries.length > 0) {
        html += `<div class="zip-section"><div class="zip-section-title">Archive Entries (${f.zipEntries.length})</div>`;
        const shown = f.zipEntries.slice(0, 30);
        for (const e of shown) {
            const name = e.isDirectory ? e.entryName + '/' : e.entryName;
            html += `<div class="zip-entry">${escapeHtml(name)} | ${formatFileSize(e.uncompressedSizeBytes)}</div>`;
        }
        if (f.zipEntries.length > 30) html += `<div class="zip-entry" style="color:var(--text-muted)">... and ${f.zipEntries.length - 30} more</div>`;
        html += '</div>';
    }

    html += '</div>';
    return html;
}

function formatFileSize(bytes) {
    if (!bytes || bytes === 0) return '0 B';
    const k = 1024, sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
}

function truncateStr(str, maxLen) {
    if (!str) return '';
    return str.length <= maxLen ? str : str.substring(0, maxLen - 2) + '..';
}

/* --- FileSyncService: Logs --- */

async function loadFileLogs() {
    try {
        const res = await fetch(`${API_BASE}/api/dashboard/filesync/logs`);
        if (!res.ok) throw new Error('offline');
        const files = await res.json();
        renderFileLogsList(files);
    } catch {
        document.getElementById('fileLogsList').innerHTML = '<p class="empty-hint">Failed to load (FileSyncService offline?)</p>';
    }
}

function renderFileLogsList(files) {
    const container = document.getElementById('fileLogsList');
    if (!files || files.length === 0) {
        container.innerHTML = '<p class="empty-hint">No log files yet</p>';
        return;
    }
    container.innerHTML = files.map(f =>
        `<div class="log-file-item" title="${escapeHtml(f)}" onclick="viewFileLog('${escapeHtml(f)}')">${escapeHtml(f)}</div>`
    ).join('');
}

async function viewFileLog(filename) {
    try {
        const res = await fetch(`${API_BASE}/api/dashboard/filesync/logs/${encodeURIComponent(filename)}`);
        if (!res.ok) throw new Error('Not found');
        const content = await res.text();
        document.getElementById('fileViewerFileName').textContent = filename;

        const mesIdx = content.indexOf('MES File Upload Log');
        const viewerEl = document.getElementById('fileViewerContent');
        const mesSection = document.getElementById('fileMesSection');

        if (mesIdx === -1) {
            viewerEl.textContent = content;
            mesSection.style.display = 'none';
        } else {
            viewerEl.textContent = content.substring(0, mesIdx);
            mesSection.innerHTML = parseMesSection(content.substring(mesIdx));
            mesSection.style.display = 'block';
        }

        document.querySelectorAll('#fileLogsList .log-file-item').forEach(el => {
            el.classList.toggle('active', el.textContent.trim() === filename);
        });
    } catch {
        document.getElementById('fileViewerContent').textContent = 'Failed to load log file.';
        document.getElementById('fileMesSection').style.display = 'none';
    }
}
