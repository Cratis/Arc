// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands;

public class when_stream_bindings_need_invocation_evidence : given.a_wiring_check
{
    void Because() => Problems = ChronicleCommandWiring.FindWiringProblems(Provider, [typeof(StreamCommand), typeof(RedirectedStreamCommand)]);
    [Fact] void should_refuse_redirected_stream_models() => Problems.Single(_ => _.CommandType == typeof(RedirectedStreamCommand)).Category.ShouldEqual("lookup");
    [Fact] void should_not_approve_unverified_routes() => Problems.Single(_ => _.CommandType == typeof(StreamCommand)).Category.ShouldEqual("unsupported");
}
