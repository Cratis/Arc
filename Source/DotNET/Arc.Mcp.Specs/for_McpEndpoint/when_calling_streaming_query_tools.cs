// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Mcp.for_McpEndpoint.given;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_calling_streaming_query_tools : an_mcp_host
{
    McpResponse[] _responses;

    protected override void ConfigureMcp(McpBuilder mcp) =>
        mcp.ExposeQuery<StreamingReadModel>(nameof(StreamingReadModel.Stream), "stream", "Streams rows.")
            .ExposeQuery<StreamingReadModel>(nameof(StreamingReadModel.Subject), "subject", "Observes rows.")
            .ExposeQuery<StreamingReadModel>(nameof(StreamingReadModel.StreamTask), "stream_task", "Streams rows asynchronously.")
            .ExposeQuery<StreamingReadModel>(nameof(StreamingReadModel.SubjectTask), "subject_task", "Observes rows asynchronously.");

    async Task Because()
    {
        Authenticate(CallerCredentialKind.PersonalAccessToken, ReadScope);
        _responses =
        [
            await CallTool("stream", "{}"),
            await CallTool("subject", "{}"),
            await CallTool("stream_task", "{}"),
            await CallTool("subject_task", "{}")
        ];
    }

    [Fact] void should_refuse_every_stream_shape() => _responses.All(_ => _.IsToolError).ShouldBeTrue();
    [Fact] void should_preserve_the_existing_generic_refusal() => _responses.All(_ => _.ToolText == McpOperations.Unsupported).ShouldBeTrue();
    [Fact] void should_never_invoke_a_streaming_performer() => _probe.Count.ShouldEqual(0);
}
