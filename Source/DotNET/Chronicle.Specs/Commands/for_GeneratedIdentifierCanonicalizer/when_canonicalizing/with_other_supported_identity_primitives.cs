// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.Commands.for_GeneratedIdentifierCanonicalizer.when_canonicalizing;

public class with_other_supported_identity_primitives : Specification
{
    string?[] _results;
    string[] _expected;

    void Establish() => _expected =
    [
        ((EventSourceId)new EventSourceId<decimal>(1234.5m)).Value,
        ((EventSourceId)new EventSourceId<long>(long.MaxValue)).Value,
        ((EventSourceId)new EventSourceId<DateTimeOffset>(new(2026, 10, 13, 9, 10, 11, TimeSpan.Zero))).Value,
        ((EventSourceId)new EventSourceId<TimeSpan>(TimeSpan.FromMinutes(5))).Value
    ];

    void Because() => _results = new object[]
    {
        new Amount(1234.5m),
        new EventSourceId<long>(long.MaxValue),
        new EventSourceId<DateTimeOffset>(new(2026, 10, 13, 9, 10, 11, TimeSpan.Zero)),
        new EventSourceId<TimeSpan>(TimeSpan.FromMinutes(5))
    }.Select(GeneratedIdentifierCanonicalizer.Canonicalize).ToArray();

    [Fact] void should_match_chronicles_invariant_identity_strings() => _results.ShouldEqual(_expected);

    public record Amount(decimal Value) : ConceptAs<decimal>(Value);
}
