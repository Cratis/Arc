// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels.for_CommandDecisionReads.when_reading_a_stream.given;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.ReadModels;
using Cratis.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels.for_DecisionReadAdmissionStartup.given;

public class a_startup : Specification
{
    private protected DecisionReadAdmissionStartup _startup;
    protected StartupReads _startupReads;
    protected DecisionReaderForSpecs _reader;
    protected IHostApplicationLifetime _lifetime;
    readonly RecordingLogger _logger = new();
    protected Dictionary<string, string?> _settings;
    protected ITypes _types;
    protected IClientArtifactsProvider _artifacts;
    protected ServiceProvider _services;
    readonly CancellationTokenSource _started = new();
    readonly CancellationTokenSource _stopping = new();

    void Establish()
    {
        _settings = [];
        _types = Substitute.For<ITypes>();
        _artifacts = Substitute.For<IClientArtifactsProvider>();
        _artifacts.ModelBoundProjections.Returns([typeof(Declarations.Balance), typeof(Declarations.StreamBalance)]);
        _reader = Substitute.For<DecisionReaderForSpecs>();
        _reader.Describe<Declarations.Balance>().Returns(new DecisionReadDescription(new(false, DecisionReadRefusalReason.Hierarchy), [], false));
        _reader.Describe<Declarations.StreamBalance>().Returns(new DecisionReadDescription(new(false, DecisionReadRefusalReason.Hierarchy), [], false));
        _reader.AdmitForStream<Declarations.StreamBalance>().Returns(new DecisionReadAdmission(true, null));
        _startupReads = new() { GetHandler = _ => Task.FromResult<IDecisionReads?>(_reader) };
        _lifetime = Substitute.For<IHostApplicationLifetime>();
        _lifetime.ApplicationStarted.Returns(_started.Token);
        _lifetime.ApplicationStopping.Returns(_stopping.Token);
    }

    protected void Configure(params Type[] commands)
    {
        var handlers = Substitute.For<ICommandHandlerProviders>();
        var commandHandlers = commands.Select(command =>
        {
            var handler = Substitute.For<ICommandHandler>();
            handler.CommandType.Returns(command);
            return handler;
        }).ToArray();
        handlers.Handlers.Returns(commandHandlers);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(_settings).Build();
        _services = new ServiceCollection().AddSingleton<IConfiguration>(configuration).BuildServiceProvider();
        _startup = new(handlers, configuration, _lifetime, _startupReads, _types, _services, _logger, _artifacts);
    }

    protected async Task StartAndCheck()
    {
        await _startup.StartAsync(CancellationToken.None);
        await SignalApplicationStarted();
        await _startup.CheckTask.WaitAsync(TimeSpan.FromSeconds(5));
    }

    protected Task SignalApplicationStarted() => _started.CancelAsync();

    protected bool Logged(LogLevel level, string text) => _logger.Entries.Exists(entry => entry.Level == level && entry.Message.Contains(text, StringComparison.Ordinal));

    protected class StartupReads : IStartupDecisionReads
    {
        public Func<CancellationToken, Task<IDecisionReads?>> GetHandler { get; set; } = _ => Task.FromResult<IDecisionReads?>(null);
        public int Requests { get; private set; }

        public Task<IDecisionReads?> Get(CancellationToken cancellationToken)
        {
            Requests++;
            return GetHandler(cancellationToken);
        }
    }

    sealed class RecordingLogger : ILogger<DecisionReadAdmissionStartup>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Entries.Add((logLevel, formatter(state, exception)));
    }

    void Destroy()
    {
        _started.Dispose();
        _stopping.Dispose();
        _services?.Dispose();
    }
}
