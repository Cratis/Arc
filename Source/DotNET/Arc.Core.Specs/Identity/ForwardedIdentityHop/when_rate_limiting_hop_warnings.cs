// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHop;

public class when_rate_limiting_hop_warnings : Specification
{
    readonly List<string> _messages = [];
    int _beforeInterval;

    void Because()
    {
        var clock = new Clock();
        var logger = new RecordingLogger(_messages);
        var request = Substitute.For<IHttpRequestContext>();
        request.Headers.Returns(new Dictionary<string, string> { [MicrosoftIdentityPlatformHeaders.PrincipalHeader] = "must-not-be-logged" });
        var diagnostics = new ForwardedIdentityHopLogging(clock);
        diagnostics.Report(logger, ForwardedIdentityHopAdmission.NoTransportCertificate, request);
        diagnostics.Report(logger, ForwardedIdentityHopAdmission.NoTransportCertificate, request);
        diagnostics.Report(logger, ForwardedIdentityHopAdmission.NoClientCertificate, request);
        _beforeInterval = _messages.Count;
        clock.Advance();
        diagnostics.Report(logger, ForwardedIdentityHopAdmission.NoTransportCertificate, request);
    }

    [Fact] void should_bound_each_reason_independently() => _beforeInterval.ShouldEqual(2);
    [Fact] void should_warn_again_after_the_interval() => _messages.Count.ShouldEqual(3);
    [Fact] void should_report_the_suppressed_count() => _messages[^1].ShouldContain("suppressed 1");
    [Fact] void should_not_log_header_values() => _messages.Exists(message => message.Contains("must-not-be-logged", StringComparison.Ordinal)).ShouldBeFalse();

    sealed class RecordingLogger(List<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => messages.Add(formatter(state, exception));
    }

    sealed class Clock : TimeProvider
    {
        DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        internal void Advance() => _now += TimeSpan.FromMinutes(1);
    }
}
