// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_an_ordinary_filter_denies_the_pipeline : given.an_evaluation
{
    CommandResult _result;

    void Establish()
    {
        var filter = Substitute.For<ICommandFilter>();
        filter.OnExecution(Arg.Any<CommandContext>()).Returns(CommandResult.Unauthorized(CorrelationId.New()));
        Services.AddSingleton<IInstancesOf<ICommandFilter>>(new KnownInstancesOf<ICommandFilter>([filter]));
    }

    async Task Because()
    {
        await using var scenario = new CommandScenario<given.RoleCommand>();
        foreach (var descriptor in Services) scenario.Services.Add(descriptor);
        scenario.WithPrincipal(Principal);
        _result = await scenario.Execute(new given.RoleCommand());
    }

    [Fact] void should_refuse_the_pipeline() => _result.IsAuthorized.ShouldBeFalse();
}
