/**
 * 全站 Admin DBGrid 標題排序
 *
 * Server 模式（分頁）：
 *   <table data-grid-sort="server"> … <th class="sortable" data-sort="imei">…</th>
 *   點擊後改 URL ?sortBy=&sortDesc= 並重整（保留其他 query）
 *
 * Client 模式（全量在 DOM）：
 *   <table data-grid-sort="client"> … <th class="sortable" data-col="0" data-type="text|num|date">
 */
(function (global) {
    'use strict';

    function ensureInd(th) {
        if (!th.querySelector('.sort-ind')) {
            var s = document.createElement('span');
            s.className = 'sort-ind';
            th.appendChild(s);
        }
    }

    function markActive(table, sortKey, desc, attr) {
        table.querySelectorAll('th.sortable').forEach(function (th) {
            th.classList.remove('sort-asc', 'sort-desc');
            ensureInd(th);
            if (th.getAttribute(attr) === sortKey)
                th.classList.add(desc ? 'sort-desc' : 'sort-asc');
        });
    }

    function initServer(table) {
        var url = new URL(location.href);
        var cur = url.searchParams.get('sortBy') || '';
        var desc = url.searchParams.get('sortDesc') === 'true';
        if (cur) markActive(table, cur, desc, 'data-sort');
        else table.querySelectorAll('th.sortable').forEach(ensureInd);

        table.querySelectorAll('th.sortable[data-sort]').forEach(function (th) {
            th.addEventListener('click', function () {
                var key = th.getAttribute('data-sort');
                if (!key) return;
                var u = new URL(location.href);
                var prev = u.searchParams.get('sortBy');
                var prevDesc = u.searchParams.get('sortDesc') === 'true';
                if (prev === key) u.searchParams.set('sortDesc', prevDesc ? 'false' : 'true');
                else {
                    u.searchParams.set('sortBy', key);
                    u.searchParams.set('sortDesc', 'false');
                }
                if (u.searchParams.has('page')) u.searchParams.set('page', '1');
                location.href = u.toString();
            });
        });
    }

    function cellValue(td, type) {
        var raw = (td.getAttribute('data-sort-value') || td.textContent || '').trim();
        if (type === 'num') {
            var n = parseFloat(raw.replace(/,/g, '').replace(/[^\d.\-]/g, ''));
            return isNaN(n) ? 0 : n;
        }
        if (type === 'date') {
            var t = Date.parse(raw);
            return isNaN(t) ? 0 : t;
        }
        return raw.toLowerCase();
    }

    function initClient(table) {
        var state = { col: -1, desc: false };
        table.querySelectorAll('thead tr:first-child th.sortable').forEach(function (th, idx) {
            if (!th.hasAttribute('data-col')) th.setAttribute('data-col', String(idx));
            ensureInd(th);
            th.addEventListener('click', function () {
                var col = parseInt(th.getAttribute('data-col'), 10);
                var type = th.getAttribute('data-type') || 'text';
                if (state.col === col) state.desc = !state.desc;
                else { state.col = col; state.desc = false; }
                markActive(table, String(col), state.desc, 'data-col');

                var tbody = table.tBodies[0];
                if (!tbody) return;
                var rows = Array.prototype.slice.call(tbody.rows);
                rows.sort(function (a, b) {
                    var av = cellValue(a.cells[col], type);
                    var bv = cellValue(b.cells[col], type);
                    var cmp = av < bv ? -1 : av > bv ? 1 : 0;
                    return state.desc ? -cmp : cmp;
                });
                rows.forEach(function (r) { tbody.appendChild(r); });
            });
        });
    }

    function autoInit(root) {
        (root || document).querySelectorAll('table[data-grid-sort="server"]').forEach(initServer);
        (root || document).querySelectorAll('table[data-grid-sort="client"]').forEach(initClient);
        // 相容：有 sortable 但未標 data-grid-sort → 預設 server（若在有分頁的列表頁）
        // data-grid-sort="off" / "sse" / "custom" 表示頁面自管排序，勿綁 location.href
        (root || document).querySelectorAll('table:not([data-grid-sort]) thead th.sortable[data-sort]').forEach(function (th) {
            var table = th.closest('table');
            if (table && !table.__gridSortBound) {
                table.__gridSortBound = true;
                table.setAttribute('data-grid-sort', 'server');
                initServer(table);
            }
        });
        // 明確標記 off/sse/custom 的表：只補排序箭頭，不綁重整
        (root || document).querySelectorAll('table[data-grid-sort="off"] thead th.sortable, table[data-grid-sort="sse"] thead th.sortable, table[data-grid-sort="custom"] thead th.sortable').forEach(ensureInd);
    }

    if (document.readyState === 'loading')
        document.addEventListener('DOMContentLoaded', function () { autoInit(document); });
    else
        autoInit(document);

    global.GridSort = { autoInit: autoInit, initServer: initServer, initClient: initClient };
})(window);
