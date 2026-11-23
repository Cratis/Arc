// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_a_protected_provider_is_missing : given.a_wiring_check
{
    void Establish() => Services.AddScoped<IDependency, Dependency>();
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(ProtectedCommand)]);
    [Fact] void should_not_approve_a_resolvable_ordinary_service() => Problems.Single().Category.ShouldEqual("unsupported");
}
