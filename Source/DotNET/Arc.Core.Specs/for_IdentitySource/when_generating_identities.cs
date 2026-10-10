// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.for_IdentitySource;

public class when_generating_identities : Specification
{
    readonly IdentitySource _source = new();
    Guid _first;
    Guid _second;

    void Because()
    {
        _first = _source.NewGuid();
        _second = _source.NewGuid();
    }

    [Fact] void should_generate_a_non_empty_identity() => _first.ShouldNotEqual(Guid.Empty);
    [Fact] void should_generate_distinct_identities() => _second.ShouldNotEqual(_first);
}
