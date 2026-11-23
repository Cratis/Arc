// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.DependencyInjection;
using Cratis.Arc.Testing.Commands;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands;

public class when_using_only_arc_rules_for_chronicle : given.a_wiring_check
{
    void Establish() => Services.AddSingleton<IInstancesOf<ICanResolveParameterDependency>>(new KnownInstancesOf<ICanResolveParameterDependency>([new ChronicleParameterDependencyResolver([typeof(Model)])]));
    void Because() => Problems = CommandWiring.FindWiringProblems(Provider, [typeof(DefaultCommand)]);
    [Fact] void should_report_the_uncovered_chronicle_resolver() => Problems.Single(_ => _.CommandType is null).Category.ShouldEqual("unsupported");
}
