// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Commands.for_CallerSubject.when_resolving;

public class and_caller_is_system : given.a_caller
{
    void Establish() => _principal = SystemPrincipal.WithRoles();
    void Because() => _result = Resolve();

    [Fact] void should_be_missing() => _result.Outcome.ShouldEqual(ReadModelKeyLookupOutcome.Missing);
}
