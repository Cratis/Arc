// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Implements the algorithm that names the graph database of an event store and a namespace.
/// </summary>
/// <remarks>
/// <para>
/// Chronicle's Neo4j graph sink uses the same algorithm, so Arc reads the database the sink writes. The steps are:
/// </para>
/// <list type="number">
/// <item>Compose <c>[prefix-]eventStore[-namespace]</c>. The <c>Default</c> namespace adds nothing. This is the unsanitized name.</item>
/// <item>Lower-case it, then replace every character outside <c>[a-z0-9-]</c> with <c>-</c>. The result is the sanitized name.</item>
/// <item>If the sanitized name does not start with a letter, prefix <c>g-</c>.</item>
/// <item>If sanitizing changed the lower-cased name, the name exceeds 63 characters or it is shorter than 3 characters, truncate to 54 characters
/// and append <c>-</c> and the first 8 lower-case hex characters of the SHA-256 of the unsanitized name as UTF-8.</item>
/// </list>
/// </remarks>
public static class GraphDatabaseName
{
    /// <summary>
    /// The name of the namespace that adds nothing to the database name.
    /// </summary>
    public const string DefaultNamespace = "Default";

    /// <summary>
    /// The longest a database name is allowed to be.
    /// </summary>
    public const int MaxLength = 63;

    /// <summary>
    /// The shortest a database name is allowed to be.
    /// </summary>
    public const int MinLength = 3;

    /// <summary>
    /// The length a name is truncated to before the hash suffix is appended.
    /// </summary>
    public const int TruncatedLength = 54;

    /// <summary>
    /// The prefix given to a name that does not start with a letter.
    /// </summary>
    public const string LetterPrefix = "g-";

    const int HashLength = 8;

    /// <summary>
    /// Get the name of the graph database.
    /// </summary>
    /// <param name="prefix">Optional prefix. Null or empty adds nothing.</param>
    /// <param name="eventStore">The name of the event store.</param>
    /// <param name="namespace">The name of the namespace. Null, empty or <see cref="DefaultNamespace"/> adds nothing.</param>
    /// <returns>The name of the database.</returns>
    public static string For(string? prefix, string eventStore, string? @namespace)
    {
        ArgumentException.ThrowIfNullOrEmpty(eventStore);

        var unsanitized = new StringBuilder();
        if (!string.IsNullOrEmpty(prefix))
        {
            unsanitized.Append(prefix).Append('-');
        }

        unsanitized.Append(eventStore);
        if (!string.IsNullOrEmpty(@namespace) && @namespace != DefaultNamespace)
        {
            unsanitized.Append('-').Append(@namespace);
        }

        var composed = unsanitized.ToString();
        var lowerCased = composed.ToLowerInvariant();
        var sanitized = Sanitize(lowerCased);
        var changed = sanitized != lowerCased;

        if (!char.IsAsciiLetterLower(sanitized[0]))
        {
            sanitized = LetterPrefix + sanitized;
        }

        if (!changed && sanitized.Length is >= MinLength and <= MaxLength)
        {
            return sanitized;
        }

        var truncated = sanitized.Length > TruncatedLength ? sanitized[..TruncatedLength] : sanitized;
        return $"{truncated}-{Hash(composed)}";
    }

    static string Sanitize(string value) =>
        string.Create(value.Length, value, static (span, source) =>
        {
            for (var i = 0; i < source.Length; i++)
            {
                var c = source[i];
                span[i] = c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' ? c : '-';
            }
        });

    static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..HashLength].ToLowerInvariant();
}
