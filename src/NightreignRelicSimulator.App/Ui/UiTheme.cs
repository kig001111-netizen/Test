namespace NightreignRelicSimulator.App.Ui;

/// <summary>
/// アプリ全体の配色・フォントを定義します。
/// </summary>
internal static class UiTheme
{
    public static readonly Color Background = Color.FromArgb(18, 18, 20);
    public static readonly Color Surface = Color.FromArgb(28, 28, 32);
    public static readonly Color SurfaceAlt = Color.FromArgb(36, 36, 42);
    public static readonly Color Border = Color.FromArgb(58, 58, 66);
    public static readonly Color Accent = Color.FromArgb(196, 140, 64);
    public static readonly Color AccentSoft = Color.FromArgb(72, 52, 28);
    public static readonly Color TextPrimary = Color.FromArgb(236, 232, 224);
    public static readonly Color TextMuted = Color.FromArgb(160, 156, 148);
    public static readonly Color Danger = Color.FromArgb(180, 72, 72);
    public static readonly Color Success = Color.FromArgb(96, 160, 112);
    public static readonly Color NavSelected = Color.FromArgb(48, 40, 28);

    public static readonly Font TitleFont = new("Segoe UI Semibold", 18F, FontStyle.Bold);
    public static readonly Font HeadingFont = new("Segoe UI Semibold", 12F, FontStyle.Bold);
    public static readonly Font BodyFont = new("Segoe UI", 9.5F, FontStyle.Regular);
    public static readonly Font MonoFont = new("Consolas", 10F, FontStyle.Regular);
    public static readonly Font ResultFont = new("Segoe UI Semibold", 22F, FontStyle.Bold);
}
