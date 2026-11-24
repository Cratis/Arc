// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_ordinary_filters_are_registered : given.an_evaluation
{
    ICommandFilter _filter;
    void Establish()
    {
        _filter = Substitute.For<ICommandFilter>();
        _filter.OnExecution(Arg.Any<CommandContext>()).Returns(CommandResult.Unauthorized(CorrelationId.New()));
        Services.AddSingleton<IInstancesOf<ICommandFilter>>(new KnownInstancesOf<ICommandFilter>([_filter]));
    }

    async Task Because() => Result = await CommandAuthorization.For<given.RoleCommand>(Provider, Principal);
    [Fact] void should_be_unsupported() => Result.ShouldBeUnsupported();
    [Fact] void should_identify_the_filter() => Result.ReasonCode.ShouldEqual("custom-filter");

    [Fact] void should_not_invoke_the_filter() => _filter.DidNotReceive().OnExecution(Arg.Any<CommandContext>());
}
