// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandHandleTaskWrappingAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandHandleTaskWrappingAnalyzer.for_ARC0010;

public class when_handle_wraps_result_in_task
{
    [Fact] async Task should_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    public record RegisterAuthor(string Name)
    {
        public Task<string> {|#0:Handle|}() => Task.FromResult(Name);
    }
}",
        VerifyCS.Diagnostic("ARC0010")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("RegisterAuthor"));
}
