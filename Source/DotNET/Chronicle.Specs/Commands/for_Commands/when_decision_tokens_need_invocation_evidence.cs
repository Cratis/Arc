// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands;

public class when_decision_tokens_need_invocation_evidence : given.a_wiring_check
{
    void Because() => Problems = ChronicleCommandWiring.FindWiringProblems(Provider, [typeof(TokenCommand), typeof(RedirectedTokenCommand)]);
    [Fact] void should_refuse_redirected_tokens() => Problems.Single(_ => _.CommandType == typeof(RedirectedTokenCommand)).Category.ShouldEqual("lookup");
    [Fact] void should_not_approve_unverified_factory_provenance() => Problems.Single(_ => _.CommandType == typeof(TokenCommand)).Category.ShouldEqual("unsupported");
}
