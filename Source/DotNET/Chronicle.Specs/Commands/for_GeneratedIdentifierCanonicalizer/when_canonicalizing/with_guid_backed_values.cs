// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.Commands.for_GeneratedIdentifierCanonicalizer.when_canonicalizing;

public class with_guid_backed_values : Specification
{
    readonly Guid _id = new(0xf89c624b, 0x56b6, 0x486d, 0x95, 0xab, 0x20, 0xa5, 0x49, 0xe3, 0x8b, 0xb0);
    string?[] _results;

    void Because() => _results = new object[] { _id, new ConceptId(_id), new TypedId(_id), new EventSourceId(_id) }.Select(GeneratedIdentifierCanonicalizer.Canonicalize).ToArray();

    [Fact] void should_use_lowercase_d_format_for_all_identity_shapes() => _results.ShouldEachConformTo(result => result == "f89c624b-56b6-486d-95ab-20a549e38bb0");

    public record ConceptId(Guid Value) : ConceptAs<Guid>(Value);
    public record TypedId(Guid Value) : EventSourceId<Guid>(Value);
}
