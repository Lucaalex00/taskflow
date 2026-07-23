using System.Text.RegularExpressions;

namespace TaskFlow.Domain.Common;

/// <summary>
/// A small, curated set of hex colors used to tag boards and users. Kept in Domain (plain
/// strings + regex, no external dependency) so both ProjectBoard and User can validate/assign
/// colors without either depending on the other or on an Infrastructure/Application concern.
/// </summary>
public static partial class ColorPalette
{
    public static readonly IReadOnlyList<string> Colors =
    [
        "#4fd1c5", "#f6ad55", "#fc8181", "#63b3ed",
        "#b794f4", "#68d391", "#f687b3", "#f6e05e"
    ];

    public static string PickRandom() => Colors[Random.Shared.Next(Colors.Count)];

    /// <summary>Deterministic pick, so the same id always maps to the same color.</summary>
    public static string PickFor(Guid id) => Colors[(int)((uint)id.GetHashCode() % (uint)Colors.Count)];

    public static bool IsValidHex(string color) => HexColorRegex().IsMatch(color);

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();
}
