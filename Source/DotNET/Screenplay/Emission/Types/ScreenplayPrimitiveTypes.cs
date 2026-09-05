// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Types;

/// <summary>
/// Holds the Screenplay primitive type names and the mapping from the fully qualified names of the framework types
/// that back them.
/// </summary>
/// <remarks>
/// This deliberately does not reuse the proxy generator's TypeScript map - TypeScript collapses every number onto
/// <c>number</c> and every date onto <c>Date</c>, while Screenplay distinguishes <c>Int</c> from <c>Decimal</c> and
/// <c>Date</c> from <c>DateTime</c>.
/// </remarks>
public static class ScreenplayPrimitiveTypes
{
#pragma warning disable CA1720 // Identifier contains type name. These are the Screenplay primitive names and cannot be renamed.
    /// <summary>
    /// The Screenplay primitive representing a universally unique identifier.
    /// </summary>
    public const string Uuid = "Uuid";

    /// <summary>
    /// The Screenplay primitive representing textual content.
    /// </summary>
    public const string String = "String";

    /// <summary>
    /// The Screenplay primitive representing a whole number.
    /// </summary>
    public const string Int = "Int";

    /// <summary>
    /// The Screenplay primitive representing a fractional number.
    /// </summary>
    public const string Decimal = "Decimal";

    /// <summary>
    /// The Screenplay primitive representing a boolean.
    /// </summary>
    public const string Bool = "Bool";

    /// <summary>
    /// The Screenplay primitive representing a date without a time component.
    /// </summary>
    public const string Date = "Date";

    /// <summary>
    /// The Screenplay primitive representing a point in time.
    /// </summary>
    public const string DateTime = "DateTime";

    /// <summary>
    /// The Screenplay type discriminator used for enumerations.
    /// </summary>
    public const string Enum = "Enum";
#pragma warning restore CA1720 // Identifier contains type name

    static readonly Dictionary<ScreenplayPrimitive, string> _names = new()
    {
        { ScreenplayPrimitive.Uuid, Uuid },
        { ScreenplayPrimitive.String, String },
        { ScreenplayPrimitive.Int, Int },
        { ScreenplayPrimitive.Decimal, Decimal },
        { ScreenplayPrimitive.Bool, Bool },
        { ScreenplayPrimitive.Date, Date },
        { ScreenplayPrimitive.DateTime, DateTime },
        { ScreenplayPrimitive.Enum, Enum }
    };

    static readonly Dictionary<string, ScreenplayPrimitive> _byFrameworkType = new(StringComparer.Ordinal)
    {
        { "System.Guid", ScreenplayPrimitive.Uuid },
        { "System.String", ScreenplayPrimitive.String },
        { "System.Char", ScreenplayPrimitive.String },
        { "System.Uri", ScreenplayPrimitive.String },
        { "System.TimeSpan", ScreenplayPrimitive.String },
        { "System.Byte", ScreenplayPrimitive.Int },
        { "System.SByte", ScreenplayPrimitive.Int },
        { "System.Int16", ScreenplayPrimitive.Int },
        { "System.UInt16", ScreenplayPrimitive.Int },
        { "System.Int32", ScreenplayPrimitive.Int },
        { "System.UInt32", ScreenplayPrimitive.Int },
        { "System.Int64", ScreenplayPrimitive.Int },
        { "System.UInt64", ScreenplayPrimitive.Int },
        { "System.Decimal", ScreenplayPrimitive.Decimal },
        { "System.Single", ScreenplayPrimitive.Decimal },
        { "System.Double", ScreenplayPrimitive.Decimal },
        { "System.Boolean", ScreenplayPrimitive.Bool },
        { "System.DateOnly", ScreenplayPrimitive.Date },
        { "System.DateTime", ScreenplayPrimitive.DateTime },
        { "System.DateTimeOffset", ScreenplayPrimitive.DateTime },
        { "System.TimeOnly", ScreenplayPrimitive.DateTime }
    };

    /// <summary>
    /// Gets the Screenplay name of a primitive.
    /// </summary>
    /// <param name="primitive">The primitive to get the name of.</param>
    /// <returns>The Screenplay type name.</returns>
    public static string GetName(ScreenplayPrimitive primitive) => _names.TryGetValue(primitive, out var name) ? name : String;

    /// <summary>
    /// Tries to resolve the Screenplay primitive backing a framework type.
    /// </summary>
    /// <param name="fullyQualifiedTypeName">The fully qualified name of the type, for example <c>System.Guid</c>.</param>
    /// <param name="primitive">The resolved primitive.</param>
    /// <returns>True when the type maps onto a Screenplay primitive.</returns>
    public static bool TryResolve(string fullyQualifiedTypeName, out ScreenplayPrimitive primitive) =>
        _byFrameworkType.TryGetValue(fullyQualifiedTypeName, out primitive);
}
