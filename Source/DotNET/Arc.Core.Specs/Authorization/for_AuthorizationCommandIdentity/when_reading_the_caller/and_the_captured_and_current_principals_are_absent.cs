// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationCommandIdentity.when_reading_the_caller;

public class and_the_captured_and_current_principals_are_absent : Specification
{
    bool _success;
    CommandCaller _caller;

    void Because()
    {
        using var frame = AuthorizationCommandIdentity.Enter(null);
        _success = AuthorizationCommandIdentity.TryGetCaller(null, out _caller, out _);
    }

    [Fact] void should_return_the_unchanged_anonymous_caller() => _success.ShouldBeTrue();
    [Fact] void should_have_no_claims() => _caller.Claims.ShouldBeEmpty();
}
