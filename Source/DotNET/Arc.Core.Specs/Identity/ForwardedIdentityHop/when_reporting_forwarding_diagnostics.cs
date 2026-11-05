// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Identity.for_ForwardedIdentityHop;

public class when_reporting_forwarding_diagnostics : Specification
{
    readonly List<string> _messages = [];
    readonly List<string> _startup = [];
    int _beforeInterval;
    int _afterCustomPaths;
    bool _validSignedConfiguration;

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
        foreach (var path in new[] { "/custom/identity", "/custom/tenants", "/.cratis/identity-details" })
        {
            var request = Substitute.For<IHttpRequestContext>();
            request.Path.Returns(path);
            diagnostics.Accepted(logger, request, "signed-v1");
        }
        _afterCustomPaths = _messages.Count;
        clock.Advance();
        var retry = Substitute.For<IHttpRequestContext>();
        retry.Path.Returns("/.cratis/me");
        diagnostics.Refused(logger, retry, "hop");
        var options = new ArcOptions { TrustForwardedIdentityHeaders = true };
        options.ForwardedIdentity.Enabled = true;
        options.ForwardedIdentity.Audience = "api";
        options.ForwardedIdentity.Keys = Enumerable.Range(0, 5).Select(index => new ForwardedIdentityKey
        {
            KeyId = $"f{index}",
            SecretBase64 = Convert.ToBase64String(new byte[32])
        }).ToList();
        var validator = new ForwardedIdentityOptionsValidator(new RecordingLogger(_startup), Substitute.For<IServiceProvider>());
        _validSignedConfiguration = validator.Validate(null, options).Succeeded;
        validator.Validate(null, options);
    }

    [Fact] void should_accept_each_form_once_and_warn_once_per_flow() => _beforeInterval.ShouldEqual(9);
    [Fact] void should_classify_custom_paths_as_the_existing_requests_flow() => _afterCustomPaths.ShouldEqual(_beforeInterval);
    [Fact] void should_warn_again_after_one_minute() => _messages.Count.ShouldEqual(10);
    [Fact] void should_distinguish_requests() => _messages.Exists(message => message.Contains("flow Requests", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_distinguish_identity_details() => _messages.Exists(message => message.Contains("flow IdentityDetails", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_distinguish_tenant_lists() => _messages.Exists(message => message.Contains("flow TenantList", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_never_include_forwarded_values() => _messages.Exists(message => message.Contains("must-not-be-logged", StringComparison.Ordinal)).ShouldBeFalse();

    [Fact] void should_start_valid_five_key_signing_without_transport_admission() => _validSignedConfiguration.ShouldBeTrue();
    [Fact] void should_warn_once_and_report_each_startup_flow() => _startup.Count.ShouldEqual(4);
    [Fact] void should_advise_transport_admission_for_signed_forwarding() => _startup.Exists(message => message.Contains("without required transport certificates", StringComparison.Ordinal)).ShouldBeTrue();

    sealed class RecordingLogger(List<string> messages) : ILogger<ForwardedIdentityOptionsValidator>
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
