// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Checks concrete pins against the strategy's exact portable encoding.
/// </summary>
internal static class GenerationFixtureShape
{
    const string Crockford = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    const string NanoIdAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";
    const string Base64Url = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
    const string Base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>
    /// Checks a fixture without drawing randomness or executing the factory.
    /// </summary>
    /// <param name="generation">The recovered strategy.</param>
    /// <param name="value">The literal fixture text.</param>
    /// <returns>Whether the fixture has the required shape.</returns>
    public static bool Matches(GenerationModel generation, string value) => generation.Kind switch
    {
        "uuid" => Guid.TryParseExact(value, "D", out _),
        "uuidv7" => UuidV7(value),
        "ulid" => CrockfordValue(value, Crockford),
        "typeid" => TypeId(value, generation.Prefix!),
        "nanoid" => value.Length == (generation.Size ?? 21) && value.All((generation.Alphabet ?? NanoIdAlphabet).Contains),
        "bytes" => Bytes(value, generation),
        _ => false
    };

    static bool UuidV7(string value) => Guid.TryParseExact(value, "D", out _) && !value.Any(character => character is >= 'A' and <= 'F') && value[14] == '7' && "89ab".Contains(value[19], StringComparison.Ordinal);

    static bool CrockfordValue(string value, string alphabet) => value.Length == 26 && value[0] is >= '0' and <= '7' && value.All(alphabet.Contains);

    static bool TypeId(string value, string prefix)
    {
        var start = prefix.Length == 0 ? string.Empty : $"{prefix}_";
        if (!value.StartsWith(start, StringComparison.Ordinal))
        {
            return false;
        }
        var encoded = value[start.Length..];
        if (!CrockfordValue(encoded, Crockford.ToLowerInvariant()))
        {
            return false;
        }

        return "ef".Contains(encoded[10], StringComparison.Ordinal) && "89abrstv".Contains(encoded[13], StringComparison.Ordinal);
    }

    static bool Bytes(string value, GenerationModel generation) => generation.Encoding switch
    {
        "hex" => value.Length == 2 * generation.Count && value.All((generation.Case == "upper" ? "0123456789ABCDEF" : "0123456789abcdef").Contains),
        "base64url" => EncodedBytes(value, generation.Count!.Value, Base64Url, 6),
        "base32" => EncodedBytes(value, generation.Count!.Value, Base32, 5),
        _ => false
    };

    static bool EncodedBytes(string value, int count, string alphabet, int bitsPerSymbol)
    {
        var symbolCount = ((8 * count) + bitsPerSymbol - 1) / bitsPerSymbol;
        if (value.Length != symbolCount || !value.All(alphabet.Contains))
        {
            return false;
        }
        var unused = (symbolCount * bitsPerSymbol) - (8 * count);

        return (alphabet.IndexOf(value[^1], StringComparison.Ordinal) & ((1 << unused) - 1)) == 0;
    }
}
