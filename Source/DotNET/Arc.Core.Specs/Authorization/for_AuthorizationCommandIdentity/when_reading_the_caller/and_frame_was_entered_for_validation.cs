// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Authorization.for_AuthorizationCommandIdentity.when_reading_the_caller;

public class and_frame_was_entered_for_validation : given.a_captured_caller
{
    void Because()
    {
        using var validation = AuthorizationCommandIdentity.EnterForValidation(_principal, null, CommandCallerProvenance.Ambient);
        _success = AuthorizationCommandIdentity.TryGetCaller(_principal, out _caller, out _refusal);
    }

    [Fact] void should_allow_the_caller() => _success.ShouldBeTrue();
    [Fact] void should_read_the_snapshot() => _caller.Single("sub").Key.ShouldEqual("represented");
}
