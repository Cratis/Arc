// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Checks concrete pins against the strategy's exact portable encoding.
/// </summary>
internal static class GenerationFixtureShape
{
    static readonly Alphabet _crockford = new("0123456789ABCDEFGHJKMNPQRSTVWXYZ", 5);
    static readonly Alphabet _typeId = new("0123456789abcdefghjkmnpqrstvwxyz", 5);
    static readonly Alphabet _nanoId = new("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-", 6);
    static readonly Alphabet _base64Url = new("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_", 6);
    static readonly Alphabet _base32 = new("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567", 5);
    static readonly Alphabet _lowerHex = new("0123456789abcdef", 4);
    static readonly Alphabet _upperHex = new("0123456789ABCDEF", 4);

    static readonly Dictionary<string, Func<Fixture, bool>> _strategies = new(StringComparer.Ordinal)
    {
        ["uuid"] = fixture => Guid.TryParseExact(fixture.Text, "D", out _),
        ["uuidv7"] = UuidV7,
        ["ulid"] = fixture => CrockfordValue(fixture, _crockford),
        ["typeid"] = TypeId,
        ["nanoid"] = NanoId,
        ["bytes"] = Bytes
    };

    static readonly Dictionary<string, Func<Fixture, bool>> _encodings = new(StringComparer.Ordinal)
    {
        ["hex"] = Hex,
        ["base64url"] = fixture => EncodedBytes(fixture, _base64Url),
        ["base32"] = fixture => EncodedBytes(fixture, _base32)
    };

    /// <summary>
    /// Checks a fixture without drawing randomness or executing the factory.
    /// </summary>
    /// <param name="generation">The recovered strategy.</param>
    /// <param name="value">The literal fixture text.</param>
    /// <returns>Whether the fixture has the required shape.</returns>
    public static bool Matches(GenerationModel generation, string value) =>
        _strategies.GetValueOrDefault(generation.Kind, Unsupported)(new(generation, value));

    static bool Unsupported(Fixture fixture) => false;

    static bool NanoId(Fixture fixture) => fixture.Text.Length == (fixture.Generation.Size ?? 21) && fixture.Text.All((fixture.Generation.Alphabet ?? _nanoId.Symbols).Contains);

    static bool UuidV7(Fixture fixture) => Guid.TryParseExact(fixture.Text, "D", out _) && !fixture.Text.Any(character => character is >= 'A' and <= 'F') && fixture.Text[14] == '7' && "89ab".Contains(fixture.Text[19], StringComparison.Ordinal);

    static bool CrockfordValue(Fixture fixture, Alphabet alphabet) => fixture.Text.Length == 26 && fixture.Text[0] is >= '0' and <= '7' && fixture.Text.All(alphabet.Symbols.Contains);

    static bool TypeId(Fixture fixture)
    {
        var prefix = fixture.Generation.Prefix!;
        var start = prefix.Length == 0 ? string.Empty : $"{prefix}_";
        if (!fixture.Text.StartsWith(start, StringComparison.Ordinal))
        {
            return false;
        }
        var encoded = fixture with { Text = fixture.Text[start.Length..] };
        if (!CrockfordValue(encoded, _typeId))
        {
            return false;
        }

        return "ef".Contains(encoded.Text[10], StringComparison.Ordinal) && "89abrstv".Contains(encoded.Text[13], StringComparison.Ordinal);
    }

    static bool Bytes(Fixture fixture) => _encodings.GetValueOrDefault(fixture.Generation.Encoding ?? string.Empty, Unsupported)(fixture);

    static bool Hex(Fixture fixture)
    {
        var alphabet = fixture.Generation.Case == "upper" ? _upperHex : _lowerHex;

        return fixture.Text.Length == 2 * fixture.Generation.Count && fixture.Text.All(alphabet.Symbols.Contains);
    }

    static bool EncodedBytes(Fixture fixture, Alphabet alphabet)
    {
        var count = fixture.Generation.Count!.Value;
        var symbolCount = ((8 * count) + alphabet.BitsPerSymbol - 1) / alphabet.BitsPerSymbol;
        if (fixture.Text.Length != symbolCount || !fixture.Text.All(alphabet.Symbols.Contains))
        {
            return false;
        }
        var unused = (symbolCount * alphabet.BitsPerSymbol) - (8 * count);

        return (alphabet.Symbols.IndexOf(fixture.Text[^1], StringComparison.Ordinal) & ((1 << unused) - 1)) == 0;
    }

    /// <summary>
    /// Carries a pinned value together with the strategy defining its shape.
    /// </summary>
    /// <param name="Generation">The concept-owned strategy.</param>
    /// <param name="Text">The literal fixture text.</param>
    sealed record Fixture(GenerationModel Generation, string Text);

    /// <summary>
    /// Carries an encoding's symbols together with their bit width.
    /// </summary>
    /// <param name="Symbols">The ordered encoding symbols.</param>
    /// <param name="BitsPerSymbol">The bit width of one encoded symbol.</param>
    sealed record Alphabet(string Symbols, int BitsPerSymbol);
}
