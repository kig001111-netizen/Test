using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using NightreignRelicSimulator.Core.Exceptions;

namespace NightreignRelicSimulator.Data.Sqlite;

/// <summary>
/// 埋め込みリソースとして配置された SQL スクリプトを読み込みます。
/// </summary>
internal static class SqlScriptLoader
{
    private const string SqlResourceFolderMarker = ".Sql.";
    private const string SchemaResourceFolderMarker = ".Sql.Schema.";

    private static readonly ConcurrentDictionary<string, string> Cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// スキーマ作成用 SQL スクリプトをファイル名順で取得します。
    /// </summary>
    /// <returns>スクリプト名と本文の組。</returns>
    public static IReadOnlyList<(string Name, string Sql)> LoadSchemaScripts()
    {
        var assembly = typeof(SqlScriptLoader).Assembly;
        var resourceNames = assembly
            .GetManifestResourceNames()
            .Where(name =>
                name.Contains(SchemaResourceFolderMarker, StringComparison.Ordinal)
                && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (resourceNames.Length == 0)
        {
            throw new DatabaseException(
                "スキーマ用 SQL スクリプトが見つかりませんでした。Data/Sql/Schema のビルド設定を確認してください。");
        }

        var scripts = new List<(string Name, string Sql)>(resourceNames.Length);
        foreach (var resourceName in resourceNames)
        {
            scripts.Add((resourceName, ReadResource(assembly, resourceName)));
        }

        return scripts;
    }

    /// <summary>
    /// 指定バージョン向けマイグレーション SQL をファイル名順で取得します。
    /// </summary>
    /// <param name="toVersion">適用先バージョン（例: 2 → Migrations/*V2*.sql または 002_*）。</param>
    public static IReadOnlyList<(string Name, string Sql)> LoadMigrationScripts(int toVersion)
    {
        var assembly = typeof(SqlScriptLoader).Assembly;
        var versionToken = toVersion.ToString("D3", System.Globalization.CultureInfo.InvariantCulture);
        var migrationMarker = ".Sql.Migrations.";

        var resourceNames = assembly
            .GetManifestResourceNames()
            .Where(name =>
                name.Contains(migrationMarker, StringComparison.Ordinal)
                && name.Contains(versionToken, StringComparison.Ordinal)
                && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (resourceNames.Length == 0)
        {
            throw new DatabaseException(
                $"マイグレーション SQL（Version={toVersion}）が見つかりませんでした。Data/Sql/Migrations を確認してください。");
        }

        var scripts = new List<(string Name, string Sql)>(resourceNames.Length);
        foreach (var resourceName in resourceNames)
        {
            scripts.Add((resourceName, ReadResource(assembly, resourceName)));
        }

        return scripts;
    }

    /// <summary>
    /// <c>Data/Sql</c> 配下の相対パスで SQL を読み込みます。
    /// </summary>
    /// <param name="relativePath">例: <c>Effect/SelectById.sql</c></param>
    /// <returns>SQL 本文。</returns>
    public static string Load(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        return Cache.GetOrAdd(relativePath, static path =>
        {
            var assembly = typeof(SqlScriptLoader).Assembly;
            var normalized = path.Replace('\\', '/').TrimStart('/');
            var resourceSuffix = SqlResourceFolderMarker + normalized.Replace('/', '.');

            var resourceName = assembly
                .GetManifestResourceNames()
                .SingleOrDefault(name =>
                    name.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));

            if (resourceName is null)
            {
                throw new DatabaseException(
                    $"SQL リソース '{normalized}' が見つかりませんでした。埋め込みリソース名の末尾が '{resourceSuffix}' となるファイルを配置してください。");
            }

            return ReadResource(assembly, resourceName);
        });
    }

    private static string ReadResource(Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            throw new DatabaseException($"SQL リソース '{resourceName}' を開けませんでした。");
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
