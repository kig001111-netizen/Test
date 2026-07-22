using Microsoft.Data.Sqlite;
using NightreignRelicSimulator.Core.Constants;
using NightreignRelicSimulator.Core.Exceptions;
using NightreignRelicSimulator.Core.Models;
using NightreignRelicSimulator.Data.Seed;

namespace NightreignRelicSimulator.Data.Sqlite;

/// <summary>
/// SQLite データベースの生成・スキーマ作成・初期データ投入を担当します。
/// </summary>
public sealed class DatabaseInitializer
{
    private static readonly object SyncRoot = new();
    private static bool _initialized;

    /// <summary>
    /// データベースを初期化します。アプリケーション起動時に一度だけ呼び出してください。
    /// </summary>
    public static void Initialize()
    {
        lock (SyncRoot)
        {
            if (_initialized)
            {
                return;
            }

            var initializer = new DatabaseInitializer();
            initializer.InitializeCore();
            _initialized = true;
        }
    }

    /// <summary>
    /// テストや明示的な再実行用に、プロセス内の初期化済みフラグをリセットします。
    /// </summary>
    internal static void ResetInitializationFlagForTests()
    {
        lock (SyncRoot)
        {
            _initialized = false;
        }
    }

    private void InitializeCore()
    {
        try
        {
            EnsureDatabaseDirectoryExists();

            using var connection = new SqliteConnection(DatabasePaths.ConnectionString);
            connection.Open();
            EnableForeignKeys(connection, enabled: true);

            // DatabaseInfo のみ先に用意し、旧スキーマならマイグレーションを先に実行する
            // （CREATE TABLE IF NOT EXISTS だと古い Effect が残り、新インデックス作成に失敗するため）
            EnsureDatabaseInfoTable(connection);

            var existingInfo = ReadDatabaseInfo(connection, transaction: null);
            if (existingInfo is not null && existingInfo.Version < DatabaseConstants.CurrentSchemaVersion)
            {
                ApplyPendingMigrations(connection, existingInfo.Version);
            }

            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    CreateSchema(connection, transaction);

                    var info = ReadDatabaseInfo(connection, transaction);
                    if (info is null)
                    {
                        EffectSeed.SeedIfEmpty(connection, transaction);
                        InsertDatabaseInfo(connection, transaction, DatabaseConstants.CurrentSchemaVersion);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
        catch (DatabaseException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new DatabaseException(
                $"データベース初期化に失敗しました。Path={DatabasePaths.DatabaseFilePath}",
                ex);
        }
    }

    private static void EnsureDatabaseDirectoryExists()
    {
        Directory.CreateDirectory(DatabasePaths.DatabaseDirectory);
    }

    private static void EnsureDatabaseInfoTable(SqliteConnection connection)
    {
        using var transaction = connection.BeginTransaction();
        try
        {
            var sql = SqlScriptLoader.Load("Schema/001_CreateDatabaseInfo.sql");
            SqlScriptExecutor.Execute(
                connection,
                transaction,
                "Schema/001_CreateDatabaseInfo.sql",
                sql);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void EnableForeignKeys(SqliteConnection connection, bool enabled)
    {
        using var command = connection.CreateCommand();
        command.CommandText = enabled ? "PRAGMA foreign_keys = ON;" : "PRAGMA foreign_keys = OFF;";
        command.ExecuteNonQuery();
    }

    private static void CreateSchema(SqliteConnection connection, SqliteTransaction transaction)
    {
        foreach (var (name, sql) in SqlScriptLoader.LoadSchemaScripts())
        {
            SqlScriptExecutor.Execute(connection, transaction, name, sql);
        }
    }

    private static DatabaseInfo? ReadDatabaseInfo(SqliteConnection connection, SqliteTransaction? transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT Version, InitializedAt
            FROM DatabaseInfo
            LIMIT 1;
            """;

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new DatabaseInfo
        {
            Version = reader.GetInt32(0),
            InitializedAt = reader.GetString(1)
        };
    }

    private static void InsertDatabaseInfo(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int version)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO DatabaseInfo (Version, InitializedAt)
            VALUES ($version, $initializedAt);
            """;
        command.Parameters.AddWithValue("$version", version);
        command.Parameters.AddWithValue("$initializedAt", DateTime.UtcNow.ToString("o"));
        command.ExecuteNonQuery();
    }

    private static void ApplyPendingMigrations(SqliteConnection connection, int currentVersion)
    {
        if (currentVersion < 2)
        {
            ApplyMigration(connection, toVersion: 2, disableForeignKeys: true, reseedEffects: true);
        }

        if (currentVersion < 3)
        {
            // Excel Seed 差し替え（スキーマ変更なし）。RelicEffect もクリアされる
            ApplyMigration(connection, toVersion: 3, disableForeignKeys: false, reseedEffects: true);
        }
    }

    private static void ApplyMigration(
        SqliteConnection connection,
        int toVersion,
        bool disableForeignKeys,
        bool reseedEffects)
    {
        if (disableForeignKeys)
        {
            EnableForeignKeys(connection, enabled: false);
        }

        try
        {
            using var transaction = connection.BeginTransaction();
            try
            {
                foreach (var (name, sql) in SqlScriptLoader.LoadMigrationScripts(toVersion))
                {
                    SqlScriptExecutor.Execute(connection, transaction, name, sql);
                }

                if (reseedEffects)
                {
                    EffectSeed.SeedAll(connection, transaction);
                }

                UpdateDatabaseInfoVersion(connection, transaction, toVersion);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        finally
        {
            if (disableForeignKeys)
            {
                EnableForeignKeys(connection, enabled: true);
            }
        }
    }

    private static void UpdateDatabaseInfoVersion(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int version)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE DatabaseInfo SET Version = $version;";
        command.Parameters.AddWithValue("$version", version);
        command.ExecuteNonQuery();
    }
}
