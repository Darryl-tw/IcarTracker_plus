using System.Text;

namespace TrackerPlus.Web.Helpers;

/// <summary>內嵌 SVG 圖示（不依賴外部 icon font）。</summary>
public static class UiIconHelper
{
    public static string Render(string name, int size = 16, string? extraClass = null)
    {
        var path = GetPath(name);
        if (path == null) return string.Empty;
        var cls = string.IsNullOrEmpty(extraClass) ? "ui-icon" : $"ui-icon {extraClass}";
        return $"""<svg class="{cls}" width="{size}" height="{size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">{path}</svg>""";
    }

    private static string? GetPath(string name) => (name ?? "").Trim().ToLowerInvariant() switch
    {
        "close" or "door-open" => """<path d="M3 21h11V3H3v18z"/><path d="M14 12h7"/><path d="M18 8l3 4-3 4"/>""", // 開啟的門 + 箭頭
        "logout" => """<path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><path d="M16 17l5-5-5-5"/><path d="M21 12H9"/>""",
        "confirm" or "save" or "check" => """<path d="M20 6 9 17l-5-5"/>""",
        "reset" or "undo" => """<path d="M3 12a9 9 0 1 0 3-6.7"/><path d="M3 3v6h6"/>""",
        "search" => """<circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/>""",
        "delete" or "trash" => """<path d="M3 6h18"/><path d="M8 6V4h8v2"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6"/><path d="M10 11v6"/><path d="M14 11v6"/>""",
        "add" or "plus" => """<path d="M12 5v14"/><path d="M5 12h14"/>""",
        "back" => """<path d="m15 18-6-6 6-6"/>""",
        "edit" or "pencil" => """<path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z"/>""",
        "details" or "eye" => """<path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"/><circle cx="12" cy="12" r="3"/>""",
        "refresh" => """<path d="M21 12a9 9 0 0 0-9-9 9.75 9.75 0 0 0-6.74 2.74L3 8"/><path d="M3 3v5h5"/><path d="M3 12a9 9 0 0 0 9 9 9.75 9.75 0 0 0 6.74-2.74L21 16"/><path d="M16 16h5v5"/>""",
        "map" or "map-pin" => """<path d="M20 10c0 6-8 12-8 12s-8-6-8-12a8 8 0 0 1 16 0Z"/><circle cx="12" cy="10" r="3"/>""",
        "history" or "clock" => """<circle cx="12" cy="12" r="10"/><path d="M12 6v6l4 2"/>""",
        "cancel" or "x" => """<path d="M18 6 6 18"/><path d="m6 6 12 12"/>""",
        "play" => """<polygon points="5 3 19 12 5 21 5 3"/>""",
        "traffic" or "road" => """<path d="M4 19h16"/><path d="M6 19V5"/><path d="M18 19V5"/><path d="M6 9h12"/><path d="M6 15h12"/>""",
        "schedule" => """<circle cx="12" cy="12" r="10"/><path d="M12 6v6l4 2"/>""",
        "unlink" => """<path d="M18 6 6 18"/><path d="M8 6h8a4 4 0 0 1 4 4v8"/>""",
        "satellite" => """<path d="M4.5 16.5c-1.5 1.26-2 5-2 5s3.74-.5 5-2c.71-.84.7-2.13-.09-2.91a2.18 2.18 0 0 0-2.91-.09z"/><path d="m12 15-3-3a22 22 0 0 1 2-3.95A12.88 12.88 0 0 1 22 2c0 2.72-.78 7.5-6 11a22.35 22.35 0 0 1-4 2z"/><path d="M9 12H4s.55-3.03 2-4c1.62-1.08 5 0 5 0"/><path d="M12 15v5s3.03-.55 4-2c1.08-1.62 0-5 0-5"/>""",
        "select-all" or "checks" => """<path d="M9 11 12 14 22 4"/><path d="M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11"/>""",
        "fit-all" or "expand" => """<path d="M15 3h6v6"/><path d="M9 21H3v-6"/><path d="M21 3l-7 7"/><path d="M3 21l7-7"/>""",
        "landmark" or "pin" => """<path d="M12 22s8-4.5 8-11a8 8 0 1 0-16 0c0 6.5 8 11 8 11z"/><circle cx="12" cy="11" r="2"/>""",
        "multi-window" or "windows" => """<rect x="3" y="3" width="8" height="8" rx="1"/><rect x="13" y="3" width="8" height="8" rx="1"/><rect x="3" y="13" width="8" height="8" rx="1"/><rect x="13" y="13" width="8" height="8" rx="1"/>""",
        "clear" or "eraser" => """<path d="m7 21-4.3-4.3c-.9-.9-.9-2.4 0-3.4l9.6-9.6c.9-.9 2.4-.9 3.4 0l4.3 4.3c.9.9.9 2.4 0 3.4L11 21"/><path d="M22 21H7"/><path d="m5 11 9 9"/>""",
        "settings" or "gear" => """<path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z"/><circle cx="12" cy="12" r="3"/>""",
        "download" or "export" => """<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><path d="M7 10l5 5 5-5"/><path d="M12 15V3"/>""",
        "prev" or "chevron-left" => """<path d="m15 18-6-6 6-6"/>""",
        "next" or "chevron-right" => """<path d="m9 18 6-6-6-6"/>""",
        "first" or "chevrons-left" => """<path d="m11 17-5-5 5-5"/><path d="m18 17-5-5 5-5"/>""",
        "last" or "chevrons-right" => """<path d="m6 17 5-5-5-5"/><path d="m13 17 5-5-5-5"/>""",
        "login" => """<path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4"/><path d="M10 17l5-5-5-5"/><path d="M15 12H3"/>""",
        "register" or "user-plus" => """<path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M19 8v6"/><path d="M22 11h-6"/>""",
        "menu" or "system" => """<path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z"/><circle cx="12" cy="12" r="3"/>""",
        "reports" or "chart" => """<path d="M3 3v18h18"/><path d="M18 17V9"/><path d="M13 17V5"/><path d="M8 17v-3"/>""",
        "help" or "info" => """<circle cx="12" cy="12" r="10"/><path d="M12 16v-4"/><path d="M12 8h.01"/>""",
        "layers" => """<path d="M12 2 2 7l10 5 10-5-10-5z"/><path d="m2 17 10 5 10-5"/><path d="m2 12 10 5 10-5"/>""",
        "list" => """<path d="M8 6h13"/><path d="M8 12h13"/><path d="M8 18h13"/><path d="M3 6h.01"/><path d="M3 12h.01"/><path d="M3 18h.01"/>""",
        "signal" => """<path d="M2 20h.01"/><path d="M7 20v-4"/><path d="M12 20v-8"/><path d="M17 20V8"/><path d="M22 4v16"/>""",
        "battery" => """<rect x="2" y="7" width="16" height="10" rx="2"/><path d="M22 11v2"/><rect x="4" y="9" width="8" height="6" fill="currentColor" stroke="none"/>""",
        "arrow-up" => """<path d="M12 19V5"/><path d="m5 12 7-7 7 7"/>""",
        _ => null
    };

    /// <summary>供 JavaScript 使用的 SVG 字串（不含外層引號轉義由呼叫端處理）。</summary>
    public static string RenderForJs(string name, int size = 16)
        => Render(name, size).Replace("'", "\\'").Replace("\r", "").Replace("\n", "");
}
