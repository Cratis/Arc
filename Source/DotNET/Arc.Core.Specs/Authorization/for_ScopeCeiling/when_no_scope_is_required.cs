// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization.given;

namespace Cratis.Arc.Authorization.for_ScopeCeiling;

public class when_no_scope_is_required : Specification
{
    bool _result;

    void Because() => _result = ScopeCeiling.Allows(verified_callers.PrincipalFor(verified_callers.Person("person")), []);

    [Fact] void should_allow() => _result.ShouldBeTrue();
}
