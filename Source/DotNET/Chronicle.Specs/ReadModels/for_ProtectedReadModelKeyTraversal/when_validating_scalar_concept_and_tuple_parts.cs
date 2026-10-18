// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyTraversal;

public class when_validating_scalar_concept_and_tuple_parts : given.a_traversal
{
    bool[] _refused;
    bool _valid;

    void Establish() => _options.Strict = true;
    void Because()
    {
        object?[] invalid = [null, string.Empty, " ", "*", "x#y", " x", "x ", new Label("*"), ("valid", "*"), new GeneratedCompositeKey("valid", "*")];
        _refused = invalid.Select(value => _traversal.IsValid(value, "wire")).ToArray();
        _valid = _traversal.IsValid((new Label("value"), new DateOnly(2026, 10, 18), 0, Guid.Empty), "wire");
    }

    [Fact] void should_refuse_all_invalid_raw_shapes() => _refused.ShouldEachConformTo(result => !result);
    [Fact] void should_accept_valid_scalars_and_tuples_without_descriptors() => _valid.ShouldBeTrue();

    record Label(string Value) : ConceptAs<string>(Value);
}
