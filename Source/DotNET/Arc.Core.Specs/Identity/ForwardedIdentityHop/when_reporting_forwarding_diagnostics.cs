// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHop;

public class when_reporting_forwarding_diagnostics : Specification
{
    readonly List<string> _messages = [];
    int _beforeInterval;

    void Because()
    {
        var clock = new Clock();
        var diagnostics = new ForwardedIdentityDiagnostics(clock);
        var logger = new RecordingLogger(_messages);
        foreach (var path in new[] { "/api/private", "/.cratis/me", "/.cratis/tenants" })
        {
            var request = Substitute.For<IHttpRequestContext>();
            request.Path.Returns(path);
            request.Headers.Returns(new Dictionary<string, string> { [MicrosoftIdentityPlatformHeaders.PrincipalHeader] = "must-not-be-logged" });
            diagnostics.Accepted(logger, request, "signed-v1");
            diagnostics.Accepted(logger, request, "signed-v1");
            diagnostics.Accepted(logger, request, "v2");
            diagnostics.Refused(logger, request, "hop");
            diagnostics.Refused(logger, request, "hop");
        }
        _beforeInterval = _messages.Count;
        clock.Advance();
        var retry = Substitute.For<IHttpRequestContext>();
        retry.Path.Returns("/.cratis/me");
        diagnostics.Refused(logger, retry, "hop");
    }

    [Fact] void should_accept_each_form_once_and_warn_once_per_flow() => _beforeInterval.ShouldEqual(9);
    [Fact] void should_warn_again_after_one_minute() => _messages.Count.ShouldEqual(10);
    [Fact] void should_distinguish_requests() => _messages.Exists(message => message.Contains("flow Requests", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_distinguish_identity_details() => _messages.Exists(message => message.Contains("flow IdentityDetails", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_distinguish_tenant_lists() => _messages.Exists(message => message.Contains("flow TenantList", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_never_include_forwarded_values() => _messages.Exists(message => message.Contains("must-not-be-logged", StringComparison.Ordinal)).ShouldBeFalse();

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
