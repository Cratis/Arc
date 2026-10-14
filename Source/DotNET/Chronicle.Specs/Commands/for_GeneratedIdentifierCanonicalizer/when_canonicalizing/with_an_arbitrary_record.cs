// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_GeneratedIdentifierCanonicalizer.when_canonicalizing;

public class with_an_arbitrary_record : Specification
{
    string? _result;

    void Because() => _result = GeneratedIdentifierCanonicalizer.Canonicalize(new DisplayId());

    [Fact] void should_not_use_the_records_display_string_as_an_identity() => _result.ShouldBeNull();

    public record DisplayId
    {
        public override string ToString() => "f89c624b-56b6-486d-95ab-20a549e38bb0";
    }
}
