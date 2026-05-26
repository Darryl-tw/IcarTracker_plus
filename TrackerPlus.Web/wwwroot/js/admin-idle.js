(function () {
    var cfg = window.adminIdleConfig;
    if (!cfg || !cfg.logoutUrl || !cfg.idleMs) return;

    var timer = null;

    function resetIdleTimer() {
        clearTimeout(timer);
        timer = setTimeout(function () {
            var form = document.getElementById('admin-idle-logout-form');
            if (form) form.submit();
            else window.location.href = cfg.logoutUrl;
        }, cfg.idleMs);
    }

    ['mousemove', 'mousedown', 'keypress', 'keydown', 'scroll', 'touchstart'].forEach(function (evt) {
        document.addEventListener(evt, resetIdleTimer, { passive: true });
    });

    resetIdleTimer();
})();
