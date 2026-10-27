// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity.for_RejectedOnBehalfOfForwards;

public class when_reporting_repeatedly : Specification
{
    List<(LogLevel Level, string Message)> _messages;
    ILogger _logger;
    int _reported;

    void Establish()
    {
        _messages = [];
        _logger = new recording_logger(_messages);
    }

    void Because()
    {
        RejectedOnBehalfOfForwards.Report(_logger, OnBehalfOfFailures.NotEnabled, ref _reported);
        RejectedOnBehalfOfForwards.Report(_logger, OnBehalfOfFailures.NotEnabled, ref _reported);
        RejectedOnBehalfOfForwards.Report(_logger, OnBehalfOfFailures.InvalidSignature, ref _reported);
    }

    [Fact] void should_warn_once() => _messages.Count(_ => _.Level == LogLevel.Warning).ShouldEqual(1);
    [Fact] void should_log_later_rejections_at_debug_level() => _messages.Count(_ => _.Level == LogLevel.Debug).ShouldEqual(2);
    [Fact] void should_say_why_the_first_was_rejected() => _messages[0].Message.ShouldContain(OnBehalfOfFailures.NotEnabled);
    [Fact] void should_say_why_a_later_one_was_rejected() => _messages[2].Message.ShouldContain(OnBehalfOfFailures.InvalidSignature);

    class recording_logger(List<(LogLevel Level, string Message)> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            messages.Add((logLevel, formatter(state, exception)));
    }
}
