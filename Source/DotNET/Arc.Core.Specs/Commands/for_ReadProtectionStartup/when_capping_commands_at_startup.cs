// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands.for_ReadProtectionStartup;

public class when_capping_commands_at_startup : Specification
{
    readonly Dictionary<string, (LogLevel Level, string Message)[]> _logs = [];
    bool _registered;

    async Task Because()
    {
        var handlers = Substitute.For<ICommandHandlerProviders>();
        var discovered = new[] { typeof(OnCommand), typeof(OnCommand), typeof(MeasureCommand), typeof(OffCommand) }.Select(type =>
        {
            var handler = Substitute.For<ICommandHandler>();
            handler.CommandType.Returns(type);
            return handler;
        }).ToArray();
        handlers.Handlers.Returns(discovered);
        foreach (var maximum in new[] { "Off", "Measure", "On" })
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ReadProtection:MaximumMode"] = maximum }).Build();
            var logger = new RecordingLogger();
            var startup = new ReadProtectionStartup(configuration, handlers, logger);
            await startup.StartAsync(CancellationToken.None);
            _logs[maximum] = logger.Entries.ToArray();
        }
        var services = new ServiceCollection().AddCratisCommands();
        _registered = services.Any(_ => _.ServiceType == typeof(IHostedService) && _.ImplementationType == typeof(ReadProtectionStartup));
    }

    [Fact] void should_warn_for_both_downgraded_modes_under_off() => _logs["Off"].Length.ShouldEqual(2);
    [Fact] void should_warn_only_for_on_under_measure() => _logs["Measure"].Length.ShouldEqual(1);
    [Fact] void should_not_warn_when_nothing_is_lowered() => _logs["On"].ShouldBeEmpty();
    [Fact] void should_use_warning_severity() => _logs["Off"].ShouldEachConformTo(_ => _.Level == LogLevel.Warning);
    [Fact] void should_name_the_command_and_configuration() => _logs["Measure"].Single().Message.ShouldContain($"{typeof(OnCommand).FullName} is lowered from On to Measure by ReadProtection:MaximumMode");
    [Fact] void should_register_the_warning_with_the_command_host() => _registered.ShouldBeTrue();

    sealed class RecordingLogger : ILogger<ReadProtectionStartup>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Entries.Add((logLevel, formatter(state, exception)));
    }

    [ReadProtection(ReadProtectionMode.On)]
    sealed class OnCommand;

    [ReadProtection(ReadProtectionMode.Measure)]
    sealed class MeasureCommand;

    [ReadProtection(ReadProtectionMode.Off)]
    sealed class OffCommand;
}
