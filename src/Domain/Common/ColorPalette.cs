using System.Text.RegularExpressions;

namespace TaskFlow.Domain.Common;

/// <summary>
/// A small, curated set of hex colors used to tag boards and users. Kept in Domain (plain
/// strings + regex, no external dependency) so both ProjectBoard and User can validate/assign
/// colors without either depending on the other or on an Infrastructure/Application concern.
/// </summary>
public static partial class ColorPalette
{
    // Curated to the app's fixed violet/pink theme — every value sits in the purple→pink→indigo
    // family so user avatars and board tags harmonize with the UI while staying distinguishable
    // from one another.
    public static readonly IReadOnlyList<string> Colors =
    [
        "#a855f7", "#ec4899", "#8b5cf6", "#d946ef",
        "#f472b6", "#818cf8", "#c084fc", "#e879f9"
    ];

    public static string PickRandom() => Colors[Random.Shared.Next(Colors.Count)];

    /// <summary>Deterministic pick, so the same id always maps to the same color.</summary>
    public static string PickFor(Guid id) => Colors[(int)((uint)id.GetHashCode() % (uint)Colors.Count)];

    public static bool IsValidHex(string color) => HexColorRegex().IsMatch(color);

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();
}
