// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandPipeline_with_events.when_executing;

public class and_the_command_declares_an_event_source_with_scope_validation_enabled : given.a_command_pipeline_with_event_handlers
{
    const string Source = "scope-validated-account";
    const string Stream = "transactions";
    ServiceProvider _root;
    IServiceScope _requestScope;
    ActivitySource _routedActivitySource;
    RecordScopeValidatedTransaction _command;
    CommandResult _result;
    EventSourceType? _resolvedEventSourceType;

    void Establish()
    {
        _command = new();
        var commandType = typeof(RecordScopeValidatedTransaction);
        var handler = new ModelBoundCommandHandler(commandType, commandType.GetMethod(nameof(RecordScopeValidatedTransaction.Handle))!);
        _commandHandlerProviders.TryGetHandlerFor(_command, out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            call[1] = handler;
            return true;
        });
        _commandFilters.OnExecution(Arg.Any<CommandContext>()).Returns(call =>
        {
            _resolvedEventSourceType = call.Arg<CommandContext>().GetEventSourceType();
            return CommandResult.Success(_correlationId);
        });

        _root = new ServiceCollection()
            .AddLogging()
            .AddSingleton(new AuthorizationDeclarations(new KnownInstancesOf<IAnonymousEvaluator>([]), new KnownInstancesOf<IAuthorizationAttributeEvaluator>([])))
            .AddScoped(_ => Substitute.For<IEventSources>())
            .AddTransient<EventSourceDefinitionValuesProvider>()
            .AddTransient<IInstancesOf<ICommandContextValuesProvider>>(services => new KnownInstancesOf<ICommandContextValuesProvider>(
                [services.GetRequiredService<EventSourceDefinitionValuesProvider>()]))
            .AddSingleton(Substitute.For<ICommandKeys>())
            .AddTransient<ICommandContextValuesBuilder, CommandContextValuesBuilder>()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _requestScope = _root.CreateScope();
        _requestScope.ServiceProvider.GetRequiredService<IEventSources>().GetFor(typeof(ScopeValidatedAccount)).Returns(
            new EventSourceDefinition(typeof(ScopeValidatedAccount), Source, "", ConcurrencyDimensions.None, [new(Stream, "", ConcurrencyDimensions.None)]));

        _routedActivitySource = new(nameof(and_the_command_declares_an_event_source_with_scope_validation_enabled));
        var activitySource = Substitute.For<IActivitySource<CommandPipeline>>();
        activitySource.ActualSource.Returns(_routedActivitySource);
        _commandPipeline = new(
            _correlationIdAccessor,
            _commandFilters,
            _commandHandlerProviders,
            _commandResponseValueHandlers,
            _commandContextModifier,
            _root.GetRequiredService<ICommandContextValuesBuilder>(),
            _commandHandlerArgumentResolver,
            new KnownInstancesOf<ICommandExecutionScope>([]),
            _root.GetRequiredService<IServiceScopeFactory>(),
            activitySource);
    }

    async Task Because() => _result = await _commandPipeline.Execute(_command, _requestScope.ServiceProvider);

    void Cleanup()
    {
        _routedActivitySource?.Dispose();
        _requestScope?.Dispose();
        _root?.Dispose();
    }

    [Fact] void should_execute_successfully_without_changing_the_principal() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_use_the_event_source_definition_from_the_request_scope() => _resolvedEventSourceType.ShouldEqual(new EventSourceType(Source));

    [Command, EventSource<ScopeValidatedAccount>(Stream)]
    public record RecordScopeValidatedTransaction
    {
        public string Handle() => "recorded";
    }

    [Cratis.Chronicle.EventSources.EventSource]
    [EventStream(Stream)]
    public class ScopeValidatedAccount : IEventSource;
}
