// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.CodeAnalysis.for_CommandConcurrencyAttributeAnalyzer.given.concurrency_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandConcurrencyAttributeAnalyzer, Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.RemoveConcurrencyArgumentCodeFixProvider>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RemoveConcurrencyArgumentCodeFixProvider;

public class when_removing_the_concurrency_argument
{
    [Theory]
    [InlineData("[EventStreamId(\"fixed\", {|#0:concurrency: true|})]", "[EventStreamId(\"fixed\")]", "EventStreamId")]
    [InlineData("[EventStreamId(\"fixed\", {|#0:true|})]", "[EventStreamId(\"fixed\")]", "EventStreamId")]
    [InlineData("[EventStreamId({|#0:concurrency: true|})]", "[EventStreamId]", "EventStreamId")]
    [InlineData("[EventStreamType(\"Stream\", {|#0:concurrency: true|})]", "[EventStreamType(\"Stream\")]", "EventStreamType")]
    [InlineData("[EventSourceType(\"Source\", {|#0:concurrency: true|})]", "[EventSourceType(\"Source\")]", "EventSourceType")]
    public async Task should_remove_only_the_flag(string attribute, string fixedAttribute, string name) =>
        await VerifyCS.VerifyCodeFixAsync(Command(attribute, "EventsWithConcurrencyScopes"), Command(fixedAttribute, "EventsWithConcurrencyScopes"), Warning(name));
}
