// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Capabilities.for_CapabilityHosting.given;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

public class when_a_gated_endpoint_declares_a_refusal_writer : a_selective_capability_host
{
    const string DefaultBody = "{\"type\":\"about:blank\",\"title\":\"Unauthorized\",\"status\":401}";

    readonly RecordingWriter _writer = new();
    int _writtenCalls;
    HttpResponseMessage _defaultRefusal;
    string _defaultBody;
    HttpResponseMessage _written;
    string _writtenBody;
    HttpStatusCode _defaultAdmitted;
    HttpStatusCode _writtenAdmitted;
    int _selectedBeforeAdmission;
    int _writtenBeforeAdmission;

    protected override CapabilityEndpointSelector Selector => new CapabilityEndpointSelector().ForRoutePattern("/mcp-spec").ForRoutePattern("/written");

    protected override void ConfigureEndpoints(WebApplication app) =>
        app.MapGet("/written", () =>
        {
            Interlocked.Increment(ref _writtenCalls);
            return "written";
        }).WithMetadata(_writer);

    async Task Because()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var cancellationToken = global::Xunit.TestContext.Current.CancellationToken;
        _defaultRefusal = await client.GetAsync($"http://127.0.0.1:{_capabilityPort}/mcp-spec", cancellationToken);
        _defaultBody = await _defaultRefusal.Content.ReadAsStringAsync(cancellationToken);
        _written = await client.GetAsync($"http://127.0.0.1:{_capabilityPort}/written", cancellationToken);
        _writtenBody = await _written.Content.ReadAsStringAsync(cancellationToken);
        _selectedBeforeAdmission = _selectedCalls;
        _writtenBeforeAdmission = _writtenCalls;

        _verified.GetCurrent().Returns(new CallerResolution(Caller(), null));
        using var defaultAdmitted = await client.GetAsync($"http://127.0.0.1:{_capabilityPort}/mcp-spec", cancellationToken);
        using var writtenAdmitted = await client.GetAsync($"http://127.0.0.1:{_capabilityPort}/written", cancellationToken);
        _defaultAdmitted = defaultAdmitted.StatusCode;
        _writtenAdmitted = writtenAdmitted.StatusCode;
    }

    [Fact] void should_refuse_without_a_writer_with_the_unchanged_status() => _defaultRefusal.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_refuse_without_a_writer_with_the_unchanged_content_type() => _defaultRefusal.Content.Headers.ContentType!.ToString().ShouldEqual("application/problem+json");
    [Fact] void should_refuse_without_a_writer_with_the_unchanged_challenge() => _defaultRefusal.Headers.WwwAuthenticate.ToString().ShouldEqual("Bearer");
    [Fact] void should_refuse_without_a_writer_with_the_unchanged_body() => _defaultBody.ShouldEqual(DefaultBody);
    [Fact] void should_let_the_writer_shape_the_refusal() => ((int)_written.StatusCode).ShouldEqual(499);
    [Fact] void should_write_the_body_of_the_writer() => _writtenBody.ShouldEqual("refused by writer");
    [Fact] void should_give_the_writer_the_status() => _writer.Refusals.Single().StatusCode.ShouldEqual(StatusCodes.Status401Unauthorized);
    [Fact] void should_give_the_writer_the_reason() => _writer.Refusals.Single().Reason.ShouldEqual(CallerRefusal.NoCredential);
    [Fact] void should_not_dispatch_a_refused_request_without_a_writer() => _selectedBeforeAdmission.ShouldEqual(0);
    [Fact] void should_not_dispatch_a_refused_request_with_a_writer() => _writtenBeforeAdmission.ShouldEqual(0);
    [Fact] void should_admit_a_verified_caller_without_a_writer() => _defaultAdmitted.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_admit_a_verified_caller_with_a_writer() => _writtenAdmitted.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_dispatch_the_admitted_request_with_a_writer() => _writtenCalls.ShouldEqual(1);

    static VerifiedCaller Caller() =>
        new("1", CallerPrincipalKind.Person, CallerCredentialKind.PersonalAccessToken, new("credential"), null, new("person"), null, null, null, null, null, "resource", null, null, null, "test", new HashSet<string>(), new HashSet<string>(), null);

    sealed class RecordingWriter : ICapabilityRefusalWriter
    {
        public List<CapabilityRefusal> Refusals { get; } = [];

        public async Task Write(HttpContext context, CapabilityRefusal refusal)
        {
            Refusals.Add(refusal);
            context.Response.StatusCode = 499;
            await context.Response.WriteAsync("refused by writer", context.RequestAborted);
        }
    }
}
