// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.for_WebApplicationBuilderExtensions.when_adding_cratis;

[Collection("UsesCurrentDirectory")]
public class and_controllers_are_turned_off : Specification
{
    WebApplication? _app;
    bool _hasApplicationParts;
    IQueryPerformerProviders? _queryPerformers;
    ICommandHandlerProviders? _commandHandlers;

    void Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddCratis(configureArcBuilder: arc => arc.WithoutControllers());
        _hasApplicationParts = builder.Services.Any(_ => _.ServiceType == typeof(ApplicationPartManager));
        _app = builder.Build();
        _queryPerformers = _app.Services.GetService<IQueryPerformerProviders>();
        _commandHandlers = _app.Services.GetService<ICommandHandlerProviders>();
    }

    void Destroy() => _app?.DisposeAsync().GetAwaiter().GetResult();

    [Fact] void should_not_discover_controllers() => _hasApplicationParts.ShouldBeFalse();
    [Fact] void should_resolve_the_query_performer_providers() => _queryPerformers.ShouldNotBeNull();
    [Fact] void should_resolve_the_command_handler_providers() => _commandHandlers.ShouldNotBeNull();
    [Fact] void should_enumerate_the_query_performers() => _queryPerformers!.Performers.ShouldNotBeNull();
    [Fact] void should_enumerate_the_command_handlers() => _commandHandlers!.Handlers.ShouldNotBeNull();
}
