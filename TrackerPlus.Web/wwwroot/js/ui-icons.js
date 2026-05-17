/** 與 UiIconHelper 對應的圖示（動態產生的按鈕用） */
window.uiIcon = function (name, size) {
    size = size || 16;
    var paths = {
        close: '<path d="M3 21h11V3H3v18z"/><path d="M14 12h7"/><path d="M18 8l3 4-3 4"/>',
        delete: '<path d="M3 6h18"/><path d="M8 6V4h8v2"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6"/><path d="M10 11v6"/><path d="M14 11v6"/>',
        confirm: '<path d="M20 6 9 17l-5-5"/>',
        reset: '<path d="M3 12a9 9 0 1 0 3-6.7"/><path d="M3 3v6h6"/>'
    };
    var p = paths[(name || '').toLowerCase()] || paths.confirm;
    return '<svg class="ui-icon" width="' + size + '" height="' + size + '" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' + p + '</svg>';
};
