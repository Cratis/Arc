// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_calling_streaming_query_tools : a_streaming_query_host
{
    McpResponse[] _responses;
    McpResponse[] _anonymous;
    McpResponse[] _insufficientScope;
    McpResponse[] _policyDenied;
    int _anonymousInvocations;
    int _insufficientScopeInvocations;
    int _policyDeniedInvocations;

    async Task Because()
    {
        _anonymous = await CallStreamingTools();
        _anonymousInvocations = _probe.Count;

        Authenticate(CallerCredentialKind.PersonalAccessToken, WriteScope);
        _insufficientScope = await CallStreamingTools();
        _insufficientScopeInvocations = _probe.Count;

        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope);
        _gate.Denied.UnionWith(StreamingTools.Select(_ => _.ToLowerInvariant()));
        _policyDenied = await CallStreamingTools();
        _policyDeniedInvocations = _probe.Count;

        _gate.Denied.Clear();
        _responses = await CallStreamingTools();
    }

    [Fact] void should_refuse_anonymous_calls() => _anonymous.All(_ => _.Status == HttpStatusCode.Unauthorized).ShouldBeTrue();
    [Fact] void should_never_invoke_a_performer_for_an_anonymous_caller() => _anonymousInvocations.ShouldEqual(0);
    [Fact] void should_refuse_callers_without_the_read_scope() => _insufficientScope.All(_ => _.Status == HttpStatusCode.Forbidden && _.RefusalError == "insufficient_scope").ShouldBeTrue();
    [Fact] void should_never_invoke_a_performer_for_a_caller_without_scope() => _insufficientScopeInvocations.ShouldEqual(0);
    [Fact] void should_refuse_policy_denied_calls() => _policyDenied.All(_ => _.Status == HttpStatusCode.Forbidden && _.RefusalError == "forbidden").ShouldBeTrue();
    [Fact] void should_never_invoke_a_performer_for_a_policy_denied_caller() => _policyDeniedInvocations.ShouldEqual(0);
    [Fact] void should_refuse_every_stream_shape() => _responses.All(_ => _.IsToolError).ShouldBeTrue();
    [Fact] void should_preserve_the_existing_generic_refusal() => _responses.All(_ => _.ToolText == McpOperations.Unsupported).ShouldBeTrue();
    [Fact] void should_never_invoke_a_streaming_performer() => _probe.Count.ShouldEqual(0);
}
