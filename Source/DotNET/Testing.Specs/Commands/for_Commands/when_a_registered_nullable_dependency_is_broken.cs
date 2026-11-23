// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_Commands;

public class when_a_registered_nullable_dependency_is_broken : given.a_wiring_check
{
    void Establish() => Services.AddScoped<IDependency>(_ => throw new InvalidOperationException("private service failure"));
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(NullableCommand)]);
    [Fact] void should_preserve_the_failure() => Problems.Single().Cause.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_include_the_exception_message_in_diagnostics() => Problems.Single().ToString().Contains("private service failure").ShouldBeFalse();
}
