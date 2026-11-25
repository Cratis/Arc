// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.DependencyInjection;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands;

public class when_the_chronicle_resolver_has_a_metadata_rule : given.a_wiring_check
{
    void Establish() => Services.AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([new ChronicleParameterDependencyResolver([typeof(Model)])]));
    void Because() => Problems = ChronicleCommandWiring.FindWiringProblems(Provider, [typeof(DefaultCommand)]);
    [Fact] void should_check_declarations_without_executing_the_resolver() => Problems.ShouldBeEmpty();
}
