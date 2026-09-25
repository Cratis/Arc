// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Authorization.for_AuthorizationConfigurationValidator;

[Collection("UsesCurrentDirectory")]
public class when_a_core_host_starts_with_an_unknown_policy : Specification
{
    Exception? _failure;
    bool _startupActionRan;

    async Task Because()
    {
        var handlers = Substitute.For<ICommandHandlerProviders>();
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(typeof(UnknownPolicyCommand));
        handlers.Handlers.Returns([handler]);
        var performers = Substitute.For<IQueryPerformerProviders>();
        performers.Performers.Returns([]);

        var builder = ArcApplication.CreateBuilder();
        builder.AddCratisArc();
        builder.Services.AddSingleton(handlers);
        builder.Services.AddSingleton(performers);
        await using var app = builder.Build();
        app.AddStartupAction(_ =>
        {
            _startupActionRan = true;
            return Task.CompletedTask;
        });
        _failure = await Catch.Exception(() => app.StartAsync());
    }

    [Fact] void should_fail_startup_before_an_action_can_start_the_listener() => _failure.ShouldBeOfExactType<InvalidAuthorizationConfiguration>();
    [Fact] void should_not_run_startup_actions() => _startupActionRan.ShouldBeFalse();

    [Authorize(Policy = "Unregistered")]
    public record UnknownPolicyCommand;
}
