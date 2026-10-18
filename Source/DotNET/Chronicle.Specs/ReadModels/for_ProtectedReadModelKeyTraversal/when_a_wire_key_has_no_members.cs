// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ProtectedReadModelKeyTraversal;

public class when_a_wire_key_has_no_members : given.a_traversal
{
    static when_a_wire_key_has_no_members() => ProtectedReadModelKeyAccessors.Register<EmptyKey>();
    bool _valid;
    bool[] _invalid;

    void Establish() => _options.Strict = true;
    void Because()
    {
        _valid = _traversal.IsValid(new EmptyKey(), "target");
        _invalid = new[] { string.Empty, "*", "x#y", " leading", "trailing " }
            .Select(wire => _traversal.IsValid(new EmptyKey(), wire)).ToArray();
    }

    [Fact] void should_distinguish_an_empty_descriptor_from_missing_coverage() => _valid.ShouldBeTrue();
    [Fact] void should_still_reject_invalid_resolved_wire_text() => _invalid.ShouldEachConformTo(result => !result);

    sealed class EmptyKey;
}
