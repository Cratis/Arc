// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Identity.Callers;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.Events;
using Cratis.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Mcp.for_McpEndpoint.given;

public class a_protected_command_host : an_mcp_host
{
    protected static readonly EventSourceId Stock = "protected-stock";
    protected readonly ProtectedCommandProbe _decision = new();
    protected readonly List<object> _appended = [];
    protected EventStoreForTesting _store;
    protected McpResponse _response;
    protected readonly ProtectedReadLogs _logs = new();
    protected virtual Type DecisionCommand => typeof(ReserveProtectedStock);
    IDisposable _subscription;

    protected override void ConfigureMcp(McpBuilder mcp) => mcp.Expose<ReserveProtectedStock>("reserve_stock", "Reserves protected stock.");

    protected override void ConfigureServices(IServiceCollection services)
    {
        var artifacts = new InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(StockChanged), typeof(StockReserved)],
            ModelBoundProjections = [typeof(ProtectedStock)]
        };
        _store = new EventStoreForTesting(null, artifacts);
        _subscription = _store.EventLog.AppendOperations.Subscribe(events =>
            _appended.AddRange(events.Where(_ => _.Result.IsSuccess).Select(_ => _.Event.Content)));

        services.AddSingleton<ILoggerProvider>(_logs);
        services.Configure<LoggerFilterOptions>(options =>
        {
            options.Rules.Clear();
            options.MinLevel = LogLevel.Trace;
        });
        services.AddSingleton(_decision);
        services.AddSingleton<IEventStore>(_store);
        services.AddSingleton(_store.EventLog);
        services.AddSingleton(_store.EventTypes);
        services.AddSingleton(_store.EventSources);
        services.AddSingleton(_store.UnitOfWorkManager);
        services.AddSingleton(_store.ReadModels);
        services.AddReadModels(artifacts);
        RegisterCommand(services, DecisionCommand);
        services.AddSingleton<IInstancesOf<ICommandExecutionScope>>(new KnownInstancesOf<ICommandExecutionScope>(new TransactionalCommandScope(), _decision));
        services.AddSingleton<IInstancesOf<ICommandResponseValueHandler>>(provider => new KnownInstancesOf<ICommandResponseValueHandler>(
            new SingleEventCommandResponseValueHandler(_store.EventLog, _store.EventTypes, new ConcurrencyScopeStrategies(new ConcurrencyOptions(), provider))));
    }

    protected async Task SeedStock()
    {
        await _store.EventLog.Append(Stock, new StockChanged(4));
        Authenticate(CallerCredentialKind.PersonalAccessToken, WriteScope);
    }

    protected async Task Reserve() => _response = await CallTool("reserve_stock", "{\"id\":\"protected-stock\"}");

    protected async Task AppendCompetingStock()
    {
        Task append;
        using (ExecutionContext.SuppressFlow())
        {
            append = Task.Run(async () => await _store.EventLog.Append(Stock, new StockChanged(2)));
        }
        await append;
    }

    protected JsonElement Outcome()
    {
        using var document = JsonDocument.Parse(_response.ToolText);
        return document.RootElement.GetProperty("outcome").Clone();
    }

    void Destroy() => _subscription?.Dispose();

    static void RegisterCommand(IServiceCollection services, Type decisionCommand)
    {
        var registered = new[] { typeof(WriteNote), decisionCommand }
            .Select(type => (ICommandHandler)new ModelBoundCommandHandler(type, type.GetMethod("Handle")!)).ToArray();
        var handlers = Substitute.For<ICommandHandlerProviders>();
        handlers.Handlers.Returns(registered);
        handlers.TryGetHandlerFor(Arg.Any<object>(), out Arg.Any<ICommandHandler?>()).Returns(call =>
        {
            var handler = registered.SingleOrDefault(candidate => candidate.CommandType == call[0]!.GetType());
            call[1] = handler;
            return handler is not null;
        });
        services.AddSingleton(handlers);
    }
}
