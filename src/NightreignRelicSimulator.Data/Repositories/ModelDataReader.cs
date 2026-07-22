using System.Data.Common;
using System.Globalization;
using NightreignRelicSimulator.Core.Enums;
using NightreignRelicSimulator.Core.Models;

namespace NightreignRelicSimulator.Data.Repositories;

/// <summary>
/// <see cref="DbDataReader"/> からモデルへ変換します。
/// </summary>
internal static class ModelDataReader
{
    public static Effect ReadEffect(DbDataReader reader)
    {
        return new Effect
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            EffectId = reader.GetInt32(reader.GetOrdinal("EffectId")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Category = reader.GetString(reader.GetOrdinal("Category")),
            CanStack = GetBoolean(reader, "CanStack"),
            Value = GetDecimal(reader, "Value"),
            Level = reader.GetInt32(reader.GetOrdinal("Level")),
            Description = reader.GetString(reader.GetOrdinal("Description")),
            DisplayOrder = reader.GetInt32(reader.GetOrdinal("DisplayOrder"))
        };
    }

    public static Relic ReadRelic(DbDataReader reader)
    {
        return new Relic
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Color = (RelicColor)reader.GetInt32(reader.GetOrdinal("Color")),
            Memo = reader.GetString(reader.GetOrdinal("Memo")),
            CreatedAt = GetDateTimeOffset(reader, "CreatedAt"),
            UpdatedAt = GetDateTimeOffset(reader, "UpdatedAt")
        };
    }

    public static RelicEffect ReadRelicEffect(DbDataReader reader)
    {
        return new RelicEffect
        {
            RelicId = reader.GetInt32(reader.GetOrdinal("RelicId")),
            SlotNumber = reader.GetInt32(reader.GetOrdinal("SlotNumber")),
            EffectId = reader.GetInt32(reader.GetOrdinal("EffectId"))
        };
    }

    public static Build ReadBuild(DbDataReader reader)
    {
        return new Build
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            CharacterName = reader.GetString(reader.GetOrdinal("CharacterName")),
            WeaponName = reader.GetString(reader.GetOrdinal("WeaponName")),
            CreatedAt = GetDateTimeOffset(reader, "CreatedAt"),
            UpdatedAt = GetDateTimeOffset(reader, "UpdatedAt")
        };
    }

    public static BuildRelic ReadBuildRelic(DbDataReader reader)
    {
        return new BuildRelic
        {
            BuildId = reader.GetInt32(reader.GetOrdinal("BuildId")),
            Position = reader.GetInt32(reader.GetOrdinal("Position")),
            RelicId = reader.GetInt32(reader.GetOrdinal("RelicId"))
        };
    }

    public static string FormatDateTimeOffset(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

    private static decimal GetDecimal(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static bool GetBoolean(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture) != 0;
    }

    private static DateTimeOffset GetDateTimeOffset(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        var text = reader.GetString(ordinal);
        return DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }
}
