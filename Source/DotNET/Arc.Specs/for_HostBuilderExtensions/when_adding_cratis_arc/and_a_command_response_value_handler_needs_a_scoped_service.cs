// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Identity;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting.for_HostBuilderExtensions.when_adding_cratis_arc;

public class and_a_command_response_value_handler_needs_a_scoped_service : Specification
{
    IHost? _host;
    Exception? _error;

    void Because()
    {
        var builder = new HostBuilder()
            .ConfigureDefaults([])
            .UseEnvironment(Environments.Development)
            .AddCratisArc(options => options.IdentityDetailsProvider = typeof(DefaultIdentityDetailsProvider));

        builder.ConfigureServices(services =>
        {
            services.AddScoped<ScopedCollaborator>();
            services.AddTransient<HandlerNeedingAScopedCollaborator>();
        });

        _host = builder.Build();
        _error = Catch.Exception(AskTheHandlersWhetherTheyCanHandleAValue);
    }

    void AskTheHandlersWhetherTheyCanHandleAValue()
    {
        using var scope = _host!.Services.CreateScope();
        var handlers = _host.Services.GetRequiredService<ICommandResponseValueHandlers>();
        var context = new CommandContext(
            CorrelationId.New(),
            typeof(object),
            new object(),
            [],
            new(),
            ServiceProvider: scope.ServiceProvider);

        handlers.CanHandle(context, "a value");
    }

    void Destroy() => _host?.Dispose();

    [Fact] void should_not_throw() => _error.ShouldBeNull();

    public class ScopedCollaborator;

    public class HandlerNeedingAScopedCollaborator(ScopedCollaborator collaborator) : ICommandResponseValueHandler
    {
        public bool CanHandle(CommandContext commandContext, object value) => collaborator is not null && value is int;

        public Task<CommandResult> Handle(CommandContext commandContext, object value) =>
            Task.FromResult(CommandResult.Success(commandContext.CorrelationId));
    }
}
