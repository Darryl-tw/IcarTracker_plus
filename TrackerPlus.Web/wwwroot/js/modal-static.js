/**
 * 全站 static Modal 規則：
 * - 僅能由確定／關閉(X)／程式呼叫 hide 關閉
 * - 點 backdrop 或 ESC → 視窗震動，不關閉
 *
 * 用法：
 *   <div class="modal" id="xxx" data-modal-static>
 *   StaticModal.show(el) / StaticModal.hide(el)
 *   StaticModal.get(el) → bootstrap.Modal instance
 */
(function (global) {
    'use strict';

    var DEFAULTS = { backdrop: 'static', keyboard: false };
    var initialized = new WeakSet();

    function resolveEl(el) {
        if (!el) return null;
        if (typeof el === 'string') {
            // 支援 '#id'、'.class'，或直接傳 id 名稱（如 'fwModal'）
            if (el.charAt(0) === '#' || el.charAt(0) === '.' || el.indexOf('[') >= 0 || el.indexOf(' ') >= 0)
                return document.querySelector(el);
            return document.getElementById(el) || document.querySelector(el);
        }
        return el;
    }

    function shake(modalEl) {
        if (!modalEl) return;
        var dialog = modalEl.querySelector('.modal-dialog') || modalEl;
        dialog.classList.remove('tp-modal-shake', 'shake');
        void dialog.offsetWidth;
        dialog.classList.add('tp-modal-shake');
        modalEl.classList.remove('modal-shake', 'tp-modal-shake');
        void modalEl.offsetWidth;
        modalEl.classList.add('tp-modal-shake');
        setTimeout(function () {
            dialog.classList.remove('tp-modal-shake', 'shake');
            modalEl.classList.remove('modal-shake', 'tp-modal-shake');
        }, 500);
    }

    function init(el) {
        el = resolveEl(el);
        if (!el || !global.bootstrap || !bootstrap.Modal) return null;
        if (initialized.has(el)) return bootstrap.Modal.getInstance(el) || bootstrap.Modal.getOrCreateInstance(el, DEFAULTS);

        el.setAttribute('data-bs-backdrop', 'static');
        el.setAttribute('data-bs-keyboard', 'false');
        el.setAttribute('data-modal-static', '');

        var instance = bootstrap.Modal.getOrCreateInstance(el, DEFAULTS);

        el.addEventListener('hidePrevented.bs.modal', function () {
            shake(el);
        });

        el.addEventListener('keydown', function (e) {
            if (e.key === 'Escape' && el.classList.contains('show')) {
                e.preventDefault();
                e.stopPropagation();
                shake(el);
            }
        });

        initialized.add(el);
        return instance;
    }

    function get(el) {
        el = resolveEl(el);
        if (!el) return null;
        return init(el);
    }

    function show(el) {
        var m = get(el);
        if (m) m.show();
        return m;
    }

    function hide(el) {
        el = resolveEl(el);
        if (!el) return null;
        var m = bootstrap.Modal.getInstance(el) || init(el);
        if (m) m.hide();
        return m;
    }

    function autoInit(root) {
        (root || document).querySelectorAll('.modal[data-modal-static], .modal.modal-locked').forEach(init);
    }

    document.addEventListener('DOMContentLoaded', function () {
        autoInit(document);
    });

    // ESC：已顯示的 static modal 震動（keyboard:false 時 Bootstrap 不處理）
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') return;
        document.querySelectorAll('.modal.show[data-modal-static], .modal.show.modal-locked').forEach(function (el) {
            shake(el);
        });
    });

    global.StaticModal = {
        defaults: DEFAULTS,
        init: init,
        get: get,
        show: show,
        hide: hide,
        shake: shake,
        autoInit: autoInit
    };
})(window);
