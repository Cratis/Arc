// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization.given;

namespace Cratis.Arc.Authorization.for_ScopeCeiling;

public class when_the_caller_lacks_one_required_scope : Specification
{
    bool _result;

    void Because() => _result = ScopeCeiling.Allows(verified_callers.PrincipalFor(verified_callers.Person("person", "a:read")), ["a:read", "b:write"]);

    [Fact] void should_refuse() => _result.ShouldBeFalse();
}
