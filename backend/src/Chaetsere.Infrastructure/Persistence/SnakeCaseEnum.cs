using System.Text.Json;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Chaetsere.Infrastructure.Persistence;

/// <summary>Maps enum members to snake_case text: BookingStatus.NoShow ↔ "no_show".</summary>
public static class EnumNames<TEnum> where TEnum : struct, Enum
{
    public static readonly IReadOnlyDictionary<TEnum, string> ToDb =
        Enum.GetValues<TEnum>().ToDictionary(v => v, v => JsonNamingPolicy.SnakeCaseLower.ConvertName(v.ToString()));

    public static readonly IReadOnlyDictionary<string, TEnum> FromDb =
        ToDb.ToDictionary(kv => kv.Value, kv => kv.Key);

    /// <summary>For CHECK constraints: 'pending', 'confirmed', …</summary>
    public static string SqlList => string.Join(", ", ToDb.Values.Select(v => $"'{v}'"));
}

public sealed class SnakeCaseEnumConverter<TEnum>() : ValueConverter<TEnum, string>(
    v => EnumNames<TEnum>.ToDb[v],
    v => EnumNames<TEnum>.FromDb[v])
    where TEnum : struct, Enum;

public static class PropertyBuilderExtensions
{
    public static PropertyBuilder<TEnum> HasSnakeCaseEnum<TEnum>(this PropertyBuilder<TEnum> builder, int maxLength = 16)
        where TEnum : struct, Enum
        => builder.HasConversion(new SnakeCaseEnumConverter<TEnum>()).HasMaxLength(maxLength);

    public static PropertyBuilder<TEnum?> HasSnakeCaseEnum<TEnum>(this PropertyBuilder<TEnum?> builder, int maxLength = 16)
        where TEnum : struct, Enum
        => builder.HasConversion(new SnakeCaseEnumConverter<TEnum>()).HasMaxLength(maxLength);

    /// <summary>Weekday stored as smallint, 0 = Sunday … 6 = Saturday (same as DayOfWeek).</summary>
    public static PropertyBuilder<DayOfWeek> HasWeekdayColumn(this PropertyBuilder<DayOfWeek> builder)
        => builder.HasConversion<short>();
}

public static class Checks
{
    public const string Phone = @"~ '^\+[1-9][0-9]{7,14}$'";

    public static string In<TEnum>(string column) where TEnum : struct, Enum
        => $"{column} IN ({EnumNames<TEnum>.SqlList})";
}
