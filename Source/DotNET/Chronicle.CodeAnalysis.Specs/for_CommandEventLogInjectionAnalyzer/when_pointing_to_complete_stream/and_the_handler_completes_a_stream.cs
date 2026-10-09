// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventLogInjectionAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventLogInjectionAnalyzer.when_pointing_to_complete_stream;

public class and_the_handler_completes_a_stream
{
    [Theory]
    [InlineData("_ = eventLog.CompleteStream(\"Stream\", \"id\");", "returning CompleteStream")]
    [InlineData("eventLog.Append(\"id\", new object());", "not IEventLog.")]
    [InlineData("", "not IEventLog.")]
    public async Task should_point_the_message_at_the_matching_alternative(string body, string advice) =>
        await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.EventSequences;

namespace TestNamespace
{
    [Command]
    public record CloseAuthor(string Name)
    {
        public void Handle(IEventLog {|#0:eventLog|})
        {
            " + body + @"
        }
    }
}",
            VerifyCS.Diagnostic("ARCCHR0007").WithSeverity(DiagnosticSeverity.Warning).WithLocation(0).WithArguments("CloseAuthor", "Handle", "eventLog", advice));
}
