// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandHandleTaskWrappingAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandHandleTaskWrappingAnalyzer.for_ARC0010;

public class when_handle_forwards_a_task
{
    [Fact] async Task should_not_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    public record RegisterAuthor(string Name)
    {
        public Task<string> Handle() => Resolve();

        static Task<string> Resolve() => Task.FromResult(""value"");
    }
}");
}
