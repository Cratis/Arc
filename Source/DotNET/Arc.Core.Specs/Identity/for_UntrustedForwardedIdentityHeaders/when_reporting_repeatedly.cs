// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity.for_UntrustedForwardedIdentityHeaders;

public class when_reporting_repeatedly : Specification
{
    ILogger _firstLogger;
    ILogger _secondLogger;
    List<(LogLevel Level, string Message)> _firstMessages;
    List<(LogLevel Level, string Message)> _secondMessages;
    int _reported;

    void Establish()
    {
        _firstMessages = [];
        _secondMessages = [];
        _firstLogger = new recording_logger(_firstMessages);
        _secondLogger = new recording_logger(_secondMessages);
    }

    void Because()
    {
        UntrustedForwardedIdentityHeaders.Report(_firstLogger, ref _reported);
        UntrustedForwardedIdentityHeaders.Report(_firstLogger, ref _reported);
        UntrustedForwardedIdentityHeaders.Report(_secondLogger, ref _reported);
    }

    [Fact] void should_log_the_first_occurrence_once() => _firstMessages.Count(message => message.Level == LogLevel.Warning).ShouldEqual(1);
    [Fact] void should_not_log_again_through_another_logger() => _secondMessages.ShouldBeEmpty();
    [Fact] void should_explain_the_opt_in() => _firstMessages.Single().Message.ShouldContain("TrustForwardedIdentityHeaders");

    class recording_logger(List<(LogLevel Level, string Message)> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            messages.Add((logLevel, formatter(state, exception)));
    }
}
