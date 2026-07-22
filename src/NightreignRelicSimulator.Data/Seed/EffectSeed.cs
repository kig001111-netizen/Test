using Microsoft.Data.Sqlite;

namespace NightreignRelicSimulator.Data.Seed;

/// <summary>
/// 効果マスタの初期データを投入します。
/// </summary>
/// <remarks>
/// 出典: 個人開発マスタ.xlsx Sheet1。
/// 倍率は百分率ではなく Value（例: 1.04）として保持します。
/// 同一名称で Level が分かれる段階効果は、共通 EffectId（先頭 ID）に正規化します。
/// </remarks>
public static class EffectSeed
{
    /// <summary>
    /// Effect テーブルが空の場合のみ初期データを投入します。
    /// </summary>
    public static int SeedIfEmpty(SqliteConnection connection, SqliteTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);

        if (CountEffects(connection, transaction) > 0)
        {
            return 0;
        }

        return SeedAll(connection, transaction);
    }

    /// <summary>
    /// 初期データを投入します（件数チェックなし）。マイグレーション後の再投入用です。
    /// </summary>
    public static int SeedAll(SqliteConnection connection, SqliteTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);

        var inserted = 0;
        foreach (var effect in CreateInitialEffects())
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO Effect
                    (EffectId, Name, Category, CanStack, Value, Level, Description, DisplayOrder)
                VALUES
                    ($effectId, $name, $category, $canStack, $value, $level, $description, $displayOrder);
                """;
            command.Parameters.AddWithValue("$effectId", effect.EffectId);
            command.Parameters.AddWithValue("$name", effect.Name);
            command.Parameters.AddWithValue("$category", effect.Category);
            command.Parameters.AddWithValue("$canStack", effect.CanStack ? 1 : 0);
            command.Parameters.AddWithValue("$value", effect.Value);
            command.Parameters.AddWithValue("$level", effect.Level);
            command.Parameters.AddWithValue("$description", effect.Description);
            command.Parameters.AddWithValue("$displayOrder", effect.DisplayOrder);
            command.ExecuteNonQuery();
            inserted++;
        }

        return inserted;
    }

    private static long CountEffects(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(*) FROM Effect;";
        return (long)(command.ExecuteScalar() ?? 0L);
    }

    private static IReadOnlyList<SeedEffectDefinition> CreateInitialEffects()
    {
        return
        [
            // 出典: 個人開発マスタ.xlsx Sheet1（倍率=Value。段階効果は同一名称を共通 EffectId）
            new(1, "物/魔/炎/雷/聖", "attack", true, 1.04m, 1, "物/魔/炎/雷/聖", 1),
            new(2, "物/魔/炎/雷/聖+1", "attack", true, 1.05m, 1, "物/魔/炎/雷/聖+1", 2),
            new(3, "物/魔/炎/雷/聖+2", "attack", true, 1.06m, 1, "物/魔/炎/雷/聖+2", 3),
            new(4, "物/魔/炎/雷/聖+3", "attack", true, 1.105m, 1, "物/魔/炎/雷/聖+3", 4),
            new(5, "物/魔/炎/雷/聖+4", "attack", true, 1.12m, 1, "物/魔/炎/雷/聖+4", 5),
            new(6, "属性", "attack", true, 1.05m, 1, "属性", 6),
            new(7, "属性+1", "attack", true, 1.08m, 1, "属性+1", 7),
            new(8, "属性+2", "attack", true, 1.1m, 1, "属性+2", 8),
            new(9, "魔術強化", "attack", true, 1.05m, 1, "魔術強化", 9),
            new(10, "魔術強化+1", "attack", true, 1.085m, 1, "魔術強化+1", 10),
            new(11, "魔術強化+2", "attack", true, 1.1m, 1, "魔術強化+2", 11),
            new(12, "祈祷強化", "attack", true, 1.05m, 1, "祈祷強化", 12),
            new(13, "祈祷強化+1", "attack", true, 1.085m, 1, "祈祷強化+1", 13),
            new(14, "祈祷強化+2", "attack", true, 1.1m, 1, "祈祷強化+2", 14),
            new(15, "ガードカウンター強化", "attack", false, 1.17m, 1, "ガードカウンター強化", 15),
            new(16, "ガードカウンター強化 +1", "attack", false, 1.25m, 1, "ガードカウンター強化 +1", 16),
            new(17, "ガードカウンター強化 +2", "attack", false, 1.29m, 1, "ガードカウンター強化 +2", 17),
            new(18, "近接攻撃力上昇", "attack", true, 1.05m, 1, "近接攻撃力上昇", 18),
            new(19, "戦技攻撃力上昇", "attack", true, 1.15m, 1, "戦技攻撃力上昇", 19),
            new(20, "通常攻撃の1段目強化", "attack", true, 1.15m, 1, "通常攻撃の1段目強化", 20),
            new(21, "攻撃連続時、攻撃力上昇", "attack", false, 1.05m, 1, "攻撃連続時、攻撃力上昇", 21),
            new(21, "攻撃連続時、攻撃力上昇", "attack", false, 1.12m, 2, "攻撃連続時、攻撃力上昇", 22),
            new(21, "攻撃連続時、攻撃力上昇", "attack", false, 1.22m, 3, "攻撃連続時、攻撃力上昇", 23),
            new(24, "致命の一撃強化", "attack", true, 1.17m, 1, "致命の一撃強化", 24),
            new(25, "致命の一撃強化+1", "attack", false, 1.24m, 1, "致命の一撃強化+1", 25),
            new(26, "咆哮とブレス強化", "attack", true, 1.15m, 1, "咆哮とブレス強化", 26),
            new(27, "武器の持ち替え時、物理攻撃力上昇", "attack", false, 1.10m, 1, "武器の持ち替え時、物理攻撃力上昇", 27),
            new(28, "属性攻撃力が付加された時、属性攻撃力上昇", "attack", true, 1.10m, 1, "属性攻撃力が付加された時、属性攻撃力上昇", 28),
            new(29, "攻撃を受けると攻撃力上昇", "attack", false, 1.15m, 1, "攻撃を受けると攻撃力上昇", 29),
            new(30, "状態異常ゲージがある時、徐々に攻撃力上昇", "attack", false, 1.45m, 1, "状態異常ゲージがある時、徐々に攻撃力上昇", 30),
            new(31, "封牢の囚を倒す度、攻撃力上昇", "attack", false, 1.05m, 1, "封牢の囚を倒す度、攻撃力上昇", 31),
            new(31, "封牢の囚を倒す度、攻撃力上昇", "attack", false, 1.10m, 2, "封牢の囚を倒す度、攻撃力上昇", 32),
            new(31, "封牢の囚を倒す度、攻撃力上昇", "attack", false, 1.15m, 3, "封牢の囚を倒す度、攻撃力上昇", 33),
            new(31, "封牢の囚を倒す度、攻撃力上昇", "attack", false, 1.20m, 4, "封牢の囚を倒す度、攻撃力上昇", 34),
            new(31, "封牢の囚を倒す度、攻撃力上昇", "attack", false, 1.25m, 5, "封牢の囚を倒す度、攻撃力上昇", 35),
            new(31, "封牢の囚を倒す度、攻撃力上昇", "attack", false, 1.30m, 6, "封牢の囚を倒す度、攻撃力上昇", 36),
            new(31, "封牢の囚を倒す度、攻撃力上昇", "attack", false, 1.35m, 7, "封牢の囚を倒す度、攻撃力上昇", 37),
            new(38, "夜の侵入者を倒す度、攻撃力上昇", "attack", false, 1.07m, 1, "夜の侵入者を倒す度、攻撃力上昇", 38),
            new(38, "夜の侵入者を倒す度、攻撃力上昇", "attack", false, 1.14m, 2, "夜の侵入者を倒す度、攻撃力上昇", 39),
            new(38, "夜の侵入者を倒す度、攻撃力上昇", "attack", false, 1.21m, 3, "夜の侵入者を倒す度、攻撃力上昇", 40),
            new(38, "夜の侵入者を倒す度、攻撃力上昇", "attack", false, 1.28m, 4, "夜の侵入者を倒す度、攻撃力上昇", 41),
            new(42, "ガードカウンター強化", "attack", true, 1.17m, 1, "ガードカウンター強化", 42),
            new(43, "脂アイテム使用時、追加で物理攻撃力上昇", "attack", false, 1.10m, 1, "脂アイテム使用時、追加で物理攻撃力上昇", 43),
            new(44, "投擲壺の攻撃力上昇", "attack", true, 1.15m, 1, "投擲壺の攻撃力上昇", 44),
            new(45, "投擲ナイフの攻撃力上昇", "attack", true, 1.14m, 1, "投擲ナイフの攻撃力上昇", 45),
            new(46, "輝石、重力石アイテムの攻撃力上昇", "attack", true, 1.15m, 1, "輝石、重力石アイテムの攻撃力上昇", 46),
            new(47, "調香術強化", "attack", true, 1.14m, 1, "調香術強化", 47),
            new(48, "○○の魔術を強化", "attack", true, 1.12m, 1, "○○の魔術を強化", 48),
            new(49, "○○の祈祷を強化", "attack", true, 1.12m, 1, "○○の祈祷を強化", 49),
            new(50, "毒状態の敵に対する攻撃を強化", "action", false, 1.10m, 1, "毒状態の敵に対する攻撃を強化", 50),
            new(51, "毒状態の敵に対する攻撃を強化+1", "action", false, 1.16m, 1, "毒状態の敵に対する攻撃を強化+1", 51),
            new(52, "毒状態の敵に対する攻撃を強化+2", "action", false, 1.20m, 1, "毒状態の敵に対する攻撃を強化+2", 52),
            new(53, "腐敗状態の敵に対する攻撃を強化", "action", false, 1.10m, 1, "腐敗状態の敵に対する攻撃を強化", 53),
            new(54, "腐敗状態の敵に対する攻撃を強化+1", "action", false, 1.16m, 1, "腐敗状態の敵に対する攻撃を強化+1", 54),
            new(55, "腐敗状態の敵に対する攻撃を強化+2", "action", false, 1.20m, 1, "腐敗状態の敵に対する攻撃を強化+2", 55),
            new(56, "凍傷状態の敵に対する攻撃を強化", "action", false, 1.10m, 1, "凍傷状態の敵に対する攻撃を強化", 56),
            new(57, "凍傷状態の敵に対する攻撃を強化+1", "action", false, 1.16m, 1, "凍傷状態の敵に対する攻撃を強化+1", 57),
            new(58, "凍傷状態の敵に対する攻撃を強化+2", "action", false, 1.20m, 1, "凍傷状態の敵に対する攻撃を強化+2", 58),
            new(59, "周囲で毒／腐敗状態の発生時、攻撃力上昇", "action", false, 1.12m, 1, "周囲で毒／腐敗状態の発生時、攻撃力上昇", 59),
            new(60, "近接3つ持ち", "action", false, 1.20m, 1, "近接3つ持ち", 60),
            new(61, "弓3つ持ち", "action", false, 1.10m, 1, "弓3つ持ち", 61),
        ];
    }

    private sealed record SeedEffectDefinition(
        int EffectId,
        string Name,
        string Category,
        bool CanStack,
        decimal Value,
        int Level,
        string Description,
        int DisplayOrder);
}
