/**
 * 全站系統提示／確定視窗（取代 alert / confirm）
 *
 * API：
 *   TpDialog.alert(message | { title, message, okText }) → Promise
 *   TpDialog.confirm(title | { title, message, yesText, cancelText }, message?) → Promise<boolean>
 *   TpDialog.confirmDelete(detail?) → Promise<boolean>  // 標題預設「確定要刪除嗎？」
 *
 * 行為：僅「是／確定／取消／X」可關閉；點外側或 ESC → 震動不關閉
 * 表單：<form data-tp-confirm data-tp-confirm-title="..." data-tp-confirm-message="...">
 */
(function (global) {
    'use strict';

    var DEFAULT_I18N = {
        yes: '是',
        cancel: '取消',
        ok: '確定',
        confirmDelete: '確定要刪除嗎？'
    };

    var rootEl = null;
    var panelEl = null;
    var titleEl = null;
    var messageEl = null;
    var yesBtn = null;
    var cancelBtn = null;
    var okBtn = null;
    var closeBtn = null;
    var activeResolve = null;
    var queue = [];

    function i18n() {
        return Object.assign({}, DEFAULT_I18N, global.TpDialogI18n || {});
    }

    function ensureDom() {
        if (rootEl) return;
        rootEl = document.createElement('div');
        rootEl.id = 'tp-dialog-root';
        rootEl.className = 'tp-dialog-overlay';
        rootEl.setAttribute('role', 'presentation');
        rootEl.innerHTML =
            '<div class="tp-dialog-panel" role="alertdialog" aria-modal="true" aria-labelledby="tp-dialog-title" aria-describedby="tp-dialog-message">' +
            '  <button type="button" class="tp-dialog-close" aria-label="Close">&times;</button>' +
            '  <svg class="tp-dialog-icon" viewBox="0 0 64 64" aria-hidden="true">' +
            '    <circle cx="32" cy="32" r="28" fill="none" stroke="#e8a060" stroke-width="2.5"/>' +
            '    <line x1="32" y1="18" x2="32" y2="38" stroke="#e8a060" stroke-width="3" stroke-linecap="round"/>' +
            '    <circle cx="32" cy="46" r="2.8" fill="#e8a060"/>' +
            '  </svg>' +
            '  <div class="tp-dialog-title" id="tp-dialog-title"></div>' +
            '  <div class="tp-dialog-message" id="tp-dialog-message"></div>' +
            '  <div class="tp-dialog-actions">' +
            '    <button type="button" class="tp-dialog-btn tp-dialog-btn-yes"></button>' +
            '    <button type="button" class="tp-dialog-btn tp-dialog-btn-cancel"></button>' +
            '    <button type="button" class="tp-dialog-btn tp-dialog-btn-ok"></button>' +
            '  </div>' +
            '</div>';
        document.body.appendChild(rootEl);
        panelEl = rootEl.querySelector('.tp-dialog-panel');
        titleEl = rootEl.querySelector('.tp-dialog-title');
        messageEl = rootEl.querySelector('.tp-dialog-message');
        yesBtn = rootEl.querySelector('.tp-dialog-btn-yes');
        cancelBtn = rootEl.querySelector('.tp-dialog-btn-cancel');
        okBtn = rootEl.querySelector('.tp-dialog-btn-ok');
        closeBtn = rootEl.querySelector('.tp-dialog-close');

        rootEl.addEventListener('click', function (e) {
            if (e.target === rootEl) shake();
        });
        panelEl.addEventListener('click', function (e) {
            e.stopPropagation();
        });
        yesBtn.addEventListener('click', function () { finish(true); });
        cancelBtn.addEventListener('click', function () { finish(false); });
        okBtn.addEventListener('click', function () { finish(true); });
        closeBtn.addEventListener('click', function () { finish(false); });
    }

    function shake() {
        if (!panelEl) return;
        panelEl.classList.remove('tp-dialog-shake');
        void panelEl.offsetWidth;
        panelEl.classList.add('tp-dialog-shake');
        setTimeout(function () {
            panelEl.classList.remove('tp-dialog-shake');
        }, 500);
    }

    function finish(result) {
        if (!activeResolve) return;
        var resolve = activeResolve;
        activeResolve = null;
        rootEl.classList.remove('tp-dialog-open');
        document.removeEventListener('keydown', onKeyDown, true);
        resolve(result);
        if (queue.length) {
            var next = queue.shift();
            openNow(next.opts, next.resolve);
        }
    }

    function onKeyDown(e) {
        if (e.key === 'Escape') {
            e.preventDefault();
            e.stopPropagation();
            shake();
        }
    }

    function openNow(opts, resolve) {
        ensureDom();
        activeResolve = resolve;
        var t = i18n();
        var mode = opts.mode || 'confirm';
        titleEl.textContent = opts.title || '';
        messageEl.textContent = opts.message || '';

        if (mode === 'alert') {
            yesBtn.style.display = 'none';
            cancelBtn.style.display = 'none';
            okBtn.style.display = '';
            okBtn.textContent = opts.okText || t.ok;
            setTimeout(function () { okBtn.focus(); }, 0);
        } else {
            okBtn.style.display = 'none';
            yesBtn.style.display = '';
            cancelBtn.style.display = '';
            yesBtn.textContent = opts.yesText || t.yes;
            cancelBtn.textContent = opts.cancelText || t.cancel;
            setTimeout(function () { yesBtn.focus(); }, 0);
        }

        rootEl.classList.add('tp-dialog-open');
        document.addEventListener('keydown', onKeyDown, true);
    }

    function open(opts) {
        return new Promise(function (resolve) {
            if (activeResolve) {
                queue.push({ opts: opts, resolve: resolve });
                return;
            }
            openNow(opts, resolve);
        });
    }

    function asOpts(titleOrOpts, message) {
        if (titleOrOpts && typeof titleOrOpts === 'object') {
            return titleOrOpts;
        }
        return { title: String(titleOrOpts == null ? '' : titleOrOpts), message: message == null ? '' : String(message) };
    }

    function alertDialog(msgOrOpts, detail) {
        var opts;
        if (msgOrOpts && typeof msgOrOpts === 'object') {
            opts = msgOrOpts;
        } else {
            // alert(msg)：單行當標題；alert(title, detail) 亦可
            var text = String(msgOrOpts == null ? '' : msgOrOpts);
            if (detail != null && String(detail).length) {
                opts = { title: text, message: String(detail) };
            } else {
                opts = { title: text, message: '' };
            }
        }
        return open({
            mode: 'alert',
            title: opts.title || '',
            message: opts.message || '',
            okText: opts.okText
        }).then(function () { return undefined; });
    }

    function confirmDialog(titleOrOpts, message) {
        var opts = asOpts(titleOrOpts, message);
        var t = i18n();
        return open({
            mode: 'confirm',
            title: opts.title || t.confirmDelete,
            message: opts.message || '',
            yesText: opts.yesText,
            cancelText: opts.cancelText
        });
    }

    function confirmDelete(detail) {
        var t = i18n();
        return confirmDialog({ title: t.confirmDelete, message: detail == null ? '' : String(detail) });
    }

    function bindConfirmForms(root) {
        (root || document).querySelectorAll('form[data-tp-confirm]').forEach(function (form) {
            if (form.getAttribute('data-tp-bound') === '1') return;
            form.setAttribute('data-tp-bound', '1');
            form.addEventListener('submit', function (e) {
                if (form.getAttribute('data-tp-confirmed') === '1') {
                    form.removeAttribute('data-tp-confirmed');
                    return;
                }
                e.preventDefault();
                e.stopPropagation();
                var title = form.getAttribute('data-tp-confirm-title') || i18n().confirmDelete;
                var msg = form.getAttribute('data-tp-confirm-message') || '';
                confirmDialog({ title: title, message: msg }).then(function (ok) {
                    if (!ok) return;
                    form.setAttribute('data-tp-confirmed', '1');
                    if (typeof form.requestSubmit === 'function') form.requestSubmit();
                    else form.submit();
                });
            });
        });
    }

    function installShims() {
        global.alert = function (msg) {
            return alertDialog(msg);
        };
        // 不覆寫同步 window.confirm（無法正確回傳 boolean）；請用 await TpDialog.confirm(...)
    }

    installShims();
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            bindConfirmForms(document);
        });
    } else {
        bindConfirmForms(document);
    }

    global.TpDialog = {
        alert: alertDialog,
        confirm: confirmDialog,
        confirmDelete: confirmDelete,
        shake: shake,
        bindConfirmForms: bindConfirmForms,
        installShims: installShims
    };
})(window);
