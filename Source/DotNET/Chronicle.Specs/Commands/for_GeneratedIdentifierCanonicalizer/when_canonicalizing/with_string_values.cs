// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.Commands.for_GeneratedIdentifierCanonicalizer.when_canonicalizing;

public class with_string_values : Specification
{
    string?[] _results;

    void Because() => _results = new object[] { "MixedCase", new StringId("F89C624B-56B6-486D-95AB-20A549E38BB0"), " spaced " }.Select(GeneratedIdentifierCanonicalizer.Canonicalize).ToArray();

    [Fact] void should_preserve_strings_exactly_including_guid_looking_values() => _results.ShouldEqual(["MixedCase", "F89C624B-56B6-486D-95AB-20A549E38BB0", " spaced "]);

    public record StringId(string Value) : ConceptAs<string>(Value);
}
