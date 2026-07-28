// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Emission.Naming;

/// <summary>
/// Represents the words a Screenplay block reads as a directive of its own rather than as a name.
/// </summary>
/// <param name="Block">The block the words belong to, named as the language names it.</param>
/// <param name="Words">The words the block reserves, in the lower camel case form names are written in.</param>
/// <remarks>
/// Screenplay is line based and every block decides what a line is from its first word. A generated name that
/// happens to be one of those words is therefore read as the directive it names, never as a name, and the language
/// offers nothing to escape it with. The sets below are read from the parsers of the language and are the only
/// reason a generated name is ever left out of a block it would otherwise belong in.
/// </remarks>
public record ReservedWords(string Block, IReadOnlySet<string> Words)
{
    /// <summary>
    /// The words the body of a <c>command</c> reads as a directive.
    /// </summary>
    public static readonly ReservedWords InCommand = new(
        "command",
        new HashSet<string>(StringComparer.Ordinal)
        {
            "authorize",
            "concurrency",
            "description",
            "handler",
            "produces",
            "validate"
        });

    /// <summary>
    /// The words the body of an <c>event</c> reads as a directive.
    /// </summary>
    public static readonly ReservedWords InEvent = new("event", new HashSet<string>(StringComparer.Ordinal) { "tag" });

    /// <summary>
    /// The words the body of a <c>produces</c> block reads as a directive.
    /// </summary>
    public static readonly ReservedWords InProduces = new("produces", new HashSet<string>(StringComparer.Ordinal) { "tag" });

    /// <summary>
    /// The words the body of a projection <c>from</c> block reads as a directive.
    /// </summary>
    public static readonly ReservedWords InFrom = new(
        "from",
        new HashSet<string>(StringComparer.Ordinal)
        {
            "key",
            "parent"
        });

    /// <summary>
    /// The words the body of a <c>concept</c> reads as a directive.
    /// </summary>
    public static readonly ReservedWords InConcept = new("concept", new HashSet<string>(StringComparer.Ordinal) { "validate" });

    /// <summary>
    /// The words no block reserves, used where a name is written somewhere nothing is dispatched on it.
    /// </summary>
    public static readonly ReservedWords None = new(string.Empty, new HashSet<string>(StringComparer.Ordinal));

    /// <summary>
    /// Gets whether the block reads a name as a directive rather than as a name.
    /// </summary>
    /// <param name="name">The name as it would be written.</param>
    /// <returns>True when the block reserves the name, false otherwise.</returns>
    public bool Reserve(string name) => Words.Contains(name);
}
