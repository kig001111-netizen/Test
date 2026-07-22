namespace NightreignRelicSimulator.Core.Constants;

/// <summary>
/// データベース関連の定数を定義します。
/// </summary>
public static class DatabaseConstants
{
    /// <summary>
    /// アプリケーションデータ配下のフォルダ名。
    /// </summary>
    public const string ApplicationFolderName = "NightreignRelicSimulator";

    /// <summary>
    /// SQLite データベースファイル名。
    /// </summary>
    public const string DatabaseFileName = "nightreign.db";

    /// <summary>
    /// 現在のスキーマバージョン。将来のマイグレーション判定に使用します。
    /// </summary>
    public const int CurrentSchemaVersion = 3;
}
