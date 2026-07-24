const API_BASE = '';
const REFRESH_INTERVAL = 5000;

let currentTab = 'overview';
let cachedRequests = [];
let logFiles = [];

document.addEventListener('DOMContentLoaded', () => {
    initNav();
    startPolling();
    loadLogs();
});

function initNav() {
    document.querySelectorAll('.nav-item').forEach(item => {
        item.addEventListener('click', () => switchTab(item.dataset.tab));
    });
    document.getElementById('refreshLogs').addEventListener('click', loadLogs);
    document.getElementById('btnRefresh').addEventListener('click', () => {
        fetchStatus();
        fetchRequests();
        if (currentTab === 'logs') loadLogs();
    });
    document.addEventListener('keydown', e => { if (e.key === 'Escape') closeDetail(); });
}

function switchTab(tab) {
    currentTab = tab;
    document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
    document.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));
    document.querySelector(`[data-tab="${tab}"]`).classList.add('active');
    document.getElementById(`tab-${tab}`).classList.add('active');
    const titles = { overview: 'Overview', requests: 'Requests', logs: 'Log Files' };
    document.getElementById('pageTitle').textContent = titles[tab] || tab;
    if (tab === 'logs') loadLogs();
}

function startPolling() {
    fetchStatus();
    fetchRequests();
    setInterval(() => {
        fetchStatus();
        fetchRequests();
    }, REFRESH_INTERVAL);
}

async function fetchStatus() {
    try {
        const res = await fetch(`${API_BASE}/api/dashboard/status`);
        const body = await res.json();
        const d = body.data || body;
        document.getElementById('statusDot').className = 'conn-dot online';
        document.getElementById('statusText').textContent = 'Running';
        document.getElementById('statTotal').textContent = d.totalRequests || 0;
        document.getElementById('statSuccess').textContent = d.successRequests || 0;
        document.getElementById('statFailed').textContent = d.failedRequests || 0;
        document.getElementById('statProcessing').textContent = d.processingRequests || 0;
        document.getElementById('navRequestCount').textContent = d.totalRequests || 0;
        document.getElementById('uptime').textContent = `Uptime: ${formatUptime(d.uptime)}`;
        document.getElementById('lastRefresh').textContent = new Date().toLocaleTimeString();
    } catch {
        document.getElementById('statusDot').className = 'conn-dot offline';
        document.getElementById('statusText').textContent = 'Offline';
    }
}

async function fetchRequests() {
    try {
        const res = await fetch(`${API_BASE}/api/dashboard/requests?count=200`);
        const body = await res.json();
        if (body.success && body.data) {
            cachedRequests = body.data;
            renderRequestsTable(body.data);
            renderActivity(body.data);
        }
    } catch { /* ignore */ }
}

function renderRequestsTable(records) {
    const tbody = document.getElementById('requestsBody');
    if (!records || records.length === 0) {
        tbody.innerHTML = '<tr class="empty-row"><td colspan="8">No requests yet...</td></tr>';
        return;
    }
    tbody.innerHTML = records.map(r => `
        <tr>
            <td><span class="mono" style="font-family:var(--font-mono);color:var(--accent);font-size:0.78rem">${r.requestId.substring(0, 8)}</span></td>
            <td>${formatTime(r.receivedTimeUtc)}</td>
            <td>${r.clientIp || '-'}</td>
            <td>${r.totalFiles}</td>
            <td><span style="color:var(--green)">${r.successCount}</span> / <span style="color:var(--red)">${r.failedCount}</span></td>
            <td>${r.durationMs != null ? r.durationMs + 'ms' : '-'}</td>
            <td>${statusBadge(r.status)}</td>
            <td><button class="btn-link" onclick="showDetail('${r.requestId}')">View Detail</button></td>
        </tr>
    `).join('');
}

function renderActivity(records) {
    const tbody = document.getElementById('overviewActivity');
    if (!records || records.length === 0) {
        tbody.innerHTML = '<tr class="empty-row"><td colspan="5">Waiting for requests...</td></tr>';
        document.getElementById('actCount').textContent = '0 records';
        return;
    }
    document.getElementById('actCount').textContent = records.length + ' records';
    tbody.innerHTML = records.slice(0, 100).map(r => {
        const isFailed = r.status === 'Failed';
        const isSuccess = r.status === 'Success';
        const isPartial = r.status === 'PartialSuccess';
        let cls = 'act-row';
        if (isFailed) cls += ' act-row-failed';
        else if (isSuccess) cls += ' act-row-success';
        else if (isPartial) cls += ' act-row-partial';
        else cls += ' act-row-processing';
        return `<tr class="${cls}" style="cursor:pointer" onclick="showDetail('${r.requestId}')">
            <td class="act-time">${formatTime(r.receivedTimeUtc)}</td>
            <td class="act-detail">${r.requestId.substring(0, 12)}</td>
            <td>${r.totalFiles}</td>
            <td><span style="color:var(--green)">${r.successCount}</span> / <span style="color:var(--red)">${r.failedCount}</span></td>
            <td>${statusBadge(r.status)}</td>
        </tr>`;
    }).join('');
}

function statusBadge(status) {
    const map = {
        'Success': 'badge-success',
        'Failed': 'badge-failed',
        'Processing': 'badge-processing',
        'PartialSuccess': 'badge-partial'
    };
    return `<span class="badge ${map[status] || 'badge-muted'}">${status || '-'}</span>`;
}

function formatUptime(seconds) {
    if (!seconds && seconds !== 0) return '-';
    const d = Math.floor(seconds / 86400);
    const h = Math.floor((seconds % 86400) / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const s = Math.floor(seconds % 60);
    if (d > 0) return `${d}d ${h}h ${m}m`;
    if (h > 0) return `${h}h ${m}m ${s}s`;
    if (m > 0) return `${m}m ${s}s`;
    return `${s}s`;
}

function formatTime(isoString) {
    if (!isoString) return '-';
    const d = new Date(isoString);
    const now = new Date();
    const diff = (now - d) / 1000;
    if (diff < 60) return `${Math.floor(diff)}s ago`;
    if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
    return d.toLocaleString('zh-CN', { month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

function escapeHtml(str) {
    if (str == null) return '';
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
}

function formatSize(bytes) {
    if (!bytes || bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
}

/* Detail Overlay */
async function showDetail(requestId) {
    try {
        const res = await fetch(`${API_BASE}/api/dashboard/requests/${encodeURIComponent(requestId)}`);
        if (!res.ok) throw new Error('Not found');
        const body = await res.json();
        if (!body.success || !body.data) throw new Error('Invalid data');
        const r = body.data;

        document.getElementById('detailMetaGrid').innerHTML = `
            <div class="detail-meta-item"><div class="meta-label">Request ID</div><div class="meta-value" style="font-family:var(--font-mono);font-size:0.78rem;color:var(--accent)">${escapeHtml(r.requestId)}</div></div>
            <div class="detail-meta-item"><div class="meta-label">Received</div><div class="meta-value">${formatTime(r.receivedTimeUtc)}</div></div>
            <div class="detail-meta-item"><div class="meta-label">Completed</div><div class="meta-value">${r.completedTimeUtc ? formatTime(r.completedTimeUtc) : '-'}</div></div>
            <div class="detail-meta-item"><div class="meta-label">Duration</div><div class="meta-value">${r.durationMs != null ? r.durationMs + ' ms' : '-'}</div></div>
            <div class="detail-meta-item"><div class="meta-label">Client IP</div><div class="meta-value">${escapeHtml(r.clientIp || '-')}</div></div>
            <div class="detail-meta-item"><div class="meta-label">Status</div><div class="meta-value">${statusBadge(r.status)}</div></div>
            <div class="detail-meta-item"><div class="meta-label">Files (OK/FAIL)</div><div class="meta-value"><span style="color:var(--green)">${r.successCount}</span> / <span style="color:var(--red)">${r.failedCount}</span></div></div>
            <div class="detail-meta-item"><div class="meta-label">Error</div><div class="meta-value" style="color:var(--red);font-size:0.78rem">${escapeHtml(r.errorMessage) || '-'}</div></div>
        `;

        const filesEl = document.getElementById('detailFiles');
        if (!r.results || r.results.length === 0) {
            filesEl.innerHTML = `<div class="file-card error"><div class="file-error">${escapeHtml(r.errorMessage) || 'No file results available.'}</div></div>`;
        } else {
            filesEl.innerHTML = r.results.map(f => renderFileCard(f)).join('');
        }

        document.getElementById('detailOverlay').classList.add('active');
    } catch {
        document.getElementById('detailOverlay').classList.add('active');
        document.getElementById('detailMetaGrid').innerHTML = '';
        document.getElementById('detailFiles').innerHTML = '<div class="file-card error"><div class="file-error">Failed to load request details.</div></div>';
    }
}

function closeDetail(e) {
    if (e && e.target !== e.currentTarget) return;
    document.getElementById('detailOverlay').classList.remove('active');
}

function renderFileCard(f) {
    const cls = f.success ? '' : ' error';
    const statusText = f.success ? 'Success' : 'Failed';
    const statusCls = f.success ? 'badge-success' : 'badge-failed';

    let html = `<div class="file-card${cls}">
        <div class="file-card-header">
            <span class="file-card-name">${f.success ? '&#128196;' : '&#9888;'} ${escapeHtml(f.fileName || 'Unknown')}</span>
            <span class="badge ${statusCls}">${statusText}</span>
        </div>
        <div class="file-card-grid">
            <span><span class="label">Inventory:</span> ${escapeHtml(f.inventroyCode || '-')}</span>
            <span><span class="label">FileCode:</span> ${escapeHtml(f.fileCode || '-')}</span>
            <span><span class="label">Size:</span> ${f.sizeBytes ? formatSize(f.sizeBytes) : '-'}</span>
            <span><span class="label">MD5:</span> <span class="mono" style="font-family:var(--font-mono);font-size:0.72rem;color:var(--accent)">${f.md5 ? f.md5.substring(0, 16) + '...' : '-'}</span></span>
            <span><span class="label">Path:</span> ${escapeHtml(f.relativePath || f.fullPath || '-')}</span>
            <span><span class="label">Modified:</span> ${f.lastModifiedTimeUtc ? formatTime(f.lastModifiedTimeUtc) : '-'}</span>
        </div>
        ${f.errorMessage ? `<div class="file-error">${escapeHtml(f.errorMessage)}</div>` : ''}`;

    // Steps timeline
    if (f.steps && f.steps.length > 0) {
        const firstTime = new Date(f.steps[0].timestampUtc).getTime();
        html += `<div class="step-section">
            <div class="step-section-title">Processing Timeline (${f.steps.length} steps)</div>
            <div class="step-grid">
                <span class="step-header">Time (UTC)</span>
                <span class="step-header">Step</span>
                <span class="step-header">Detail</span>
                <span class="step-header">Elapsed</span>`;
        for (const s of f.steps) {
            const t = new Date(s.timestampUtc);
            const elapsed = t.getTime() - firstTime;
            const stepCls = s.step.startsWith('ERROR') || s.step.startsWith('UNEXPECTED') || s.step.startsWith('HTTP_ERROR') || s.step.startsWith('TIMEOUT') || s.step.startsWith('SECURITY') ? 'step-error'
                : s.step.startsWith('SUCCESS') || s.step.startsWith('DOWNLOAD_COMPLETE') || s.step.startsWith('MD5_COMPLETE') || s.step.startsWith('ZIP_EXTRACT_COMPLETE') || s.step.startsWith('FILE_FOUND') ? 'step-ok' : '';
            html += `<span class="step-cell ${stepCls}" title="${escapeHtml(s.step)}">${escapeHtml(s.step)}</span>
                <span class="step-cell step-detail" title="${escapeHtml(s.detail || '')}">${escapeHtml(truncate(s.detail, 60))}</span>
                <span class="step-cell step-time">${t.toLocaleTimeString('zh-CN', { hour12: false })}</span>
                <span class="step-cell step-elapsed">+${elapsed}ms</span>`;
        }
        html += `</div></div>`;
    }

    // ZIP entries
    if (f.isZipFile && f.zipEntries && f.zipEntries.length > 0) {
        html += `<div class="zip-section">
            <div class="zip-section-title">ZIP Archive (${f.zipEntries.length} entries)</div>`;
        const shown = f.zipEntries.slice(0, 30);
        for (const e of shown) {
            const name = e.isDirectory ? e.entryName + '/' : e.entryName;
            const md5 = e.isDirectory ? '-' : (e.md5 ? e.md5.substring(0, 12) + '...' : '-');
            html += `<div class="zip-entry">${e.isDirectory ? '&#128193;' : '&#128196;'} ${escapeHtml(name)} &nbsp;|&nbsp; ${formatSize(e.uncompressedSizeBytes)} &nbsp;|&nbsp; ${md5}</div>`;
        }
        if (f.zipEntries.length > 30) {
            html += `<div class="zip-entry" style="color:var(--text-muted)">... and ${f.zipEntries.length - 30} more entries</div>`;
        }
        html += `</div>`;
    }

    html += `</div>`;
    return html;
}

function truncate(str, maxLen) {
    if (!str) return '';
    return str.length <= maxLen ? str : str.substring(0, maxLen - 2) + '..';
}

/* Logs */
async function loadLogs() {
    try {
        const res = await fetch(`${API_BASE}/api/dashboard/logs`);
        logFiles = await res.json();
        renderLogsList();
    } catch {
        document.getElementById('logsList').innerHTML = '<p class="empty-hint">Failed to load logs</p>';
    }
}

function renderLogsList() {
    const container = document.getElementById('logsList');
    if (!logFiles || logFiles.length === 0) {
        container.innerHTML = '<p class="empty-hint">No log files yet</p>';
        return;
    }
    container.innerHTML = logFiles.map(f =>
        `<div class="log-file-item" title="${f}" onclick="viewLog('${escapeHtml(f)}')">${f}</div>`
    ).join('');
}

async function viewLog(filename) {
    const loadingEl = document.getElementById('logViewerLoading');
    const viewerEl = document.getElementById('viewerContent');
    const mesSection = document.getElementById('mesSection');

    loadingEl.style.display = 'flex';
    viewerEl.textContent = '';
    mesSection.style.display = 'none';

    try {
        const res = await fetch(`${API_BASE}/api/dashboard/logs/${encodeURIComponent(filename)}`);
        if (!res.ok) throw new Error('Not found');
        const content = await res.text();
        document.getElementById('viewerFileName').textContent = filename;

        const mesIdx = content.indexOf('MES File Upload Log');
        if (mesIdx === -1) {
            viewerEl.textContent = content;
        } else {
            viewerEl.textContent = content.substring(0, mesIdx);
            mesSection.innerHTML = parseMesSection(content.substring(mesIdx));
            mesSection.style.display = 'block';
        }

        document.querySelectorAll('.log-file-item').forEach(el => {
            el.classList.toggle('active', el.textContent.trim() === filename);
        });

        if (currentTab !== 'logs') switchTab('logs');
    } catch {
        viewerEl.textContent = 'Failed to load log file.';
    } finally {
        loadingEl.style.display = 'none';
    }
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
        if (t === '--- Request Body ---') { mode = 'request'; continue; }
        if (t === '--- Response Body ---') { mode = 'response'; continue; }
        if (t.startsWith('===')) { if (mode === 'response') mode = 'done'; continue; }
        if (mode === 'request') reqLines.push(line);
        else if (mode === 'response') resLines.push(line);
    }

    const isFail = httpStatus === 'N/A (exception)' || httpStatus.startsWith('4') || httpStatus.startsWith('5');
    const isOk = !isFail && httpStatus !== '';
    const blocCls = isOk ? 'mes-ok' : 'mes-fail';
    const label = isOk ? 'Upload Successful' : 'Upload Failed';
    let reqBody = reqLines.join('\n').trim();
    let resBody = resLines.join('\n').trim();

    // Replace base64 file content with placeholder for readability.
    reqBody = hideBase64(reqBody);
    resBody = hideBase64(resBody);
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

function hideBase64(str) {
    // Replace base64 content in "file_content": "..." with "[base64]"
    return str.replace(/"file_content":\s*"[^"]{100,}"/g, '"file_content": "[base64]"');
}
