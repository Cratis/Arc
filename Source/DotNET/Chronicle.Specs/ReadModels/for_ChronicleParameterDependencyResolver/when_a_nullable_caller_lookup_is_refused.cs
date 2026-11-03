// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleParameterDependencyResolver;

public class when_a_nullable_caller_lookup_is_refused : given.a_parameter_lookup
{
    Exception? _exception;

    void Establish() => Configure(new CallerCommand());

    async Task Because()
    {
        using var frame = AuthorizationCommandIdentity.Enter(new ClaimsPrincipal(new ClaimsIdentity([], "test")));
        _exception = await Catch.Exception(ResolveHandle);
    }

    [Fact] void should_deny_instead_of_supplying_null() => _exception.ShouldBeOfExactType<ReadModelKeyResolutionDenied>();
    [Fact] void should_not_acquire_a_model() => _readModels.ReceivedCalls().ShouldBeEmpty();

    public class CallerCommand
    {
        public void Handle([LookUpBy<CallerSubject>] Balance? balance) { }
    }
}
