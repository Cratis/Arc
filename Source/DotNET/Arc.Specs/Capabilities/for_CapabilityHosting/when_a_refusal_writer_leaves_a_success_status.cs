// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Capabilities.for_CapabilityHosting.given;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

public class when_a_refusal_writer_leaves_a_success_status : a_selective_capability_host
{
    int _writtenCalls;
    HttpResponseMessage _refused;
    string _body;
    HttpResponseMessage _resetToSuccess;

    protected override CapabilityEndpointSelector Selector => new CapabilityEndpointSelector().ForRoutePattern("/mcp-spec").ForRoutePattern("/forgetful").ForRoutePattern("/resetting");

    protected override void ConfigureEndpoints(WebApplication app)
    {
        app.MapGet("/forgetful", () =>
        {
            Interlocked.Increment(ref _writtenCalls);
            return "forgetful";
        }).WithMetadata(new ForgetfulWriter());
        app.MapGet("/resetting", () =>
        {
            Interlocked.Increment(ref _writtenCalls);
            return "resetting";
        }).WithMetadata(new ResettingWriter());
    }

    async Task Because()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var cancellationToken = global::Xunit.TestContext.Current.CancellationToken;
        _refused = await client.GetAsync($"http://127.0.0.1:{_capabilityPort}/forgetful", cancellationToken);
        _body = await _refused.Content.ReadAsStringAsync(cancellationToken);
        _resetToSuccess = await client.GetAsync($"http://127.0.0.1:{_capabilityPort}/resetting", cancellationToken);
    }

    [Fact] void should_still_refuse_with_the_refusal_status() => _refused.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_keep_what_the_writer_wrote() => _body.ShouldEqual("refused without a status");
    [Fact] void should_put_the_refusal_status_back_when_the_writer_sets_a_success_status() => _resetToSuccess.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_dispatch_the_refused_request() => _writtenCalls.ShouldEqual(0);

    sealed class ResettingWriter : ICapabilityRefusalWriter
    {
        public Task Write(HttpContext context, CapabilityRefusal refusal)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        }
    }

    sealed class ForgetfulWriter : ICapabilityRefusalWriter
    {
        public Task Write(HttpContext context, CapabilityRefusal refusal) =>
            context.Response.WriteAsync("refused without a status", context.RequestAborted);
    }
}
