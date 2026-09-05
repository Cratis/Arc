// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandHandleTaskWrappingAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandHandleTaskWrappingAnalyzer.for_ARC0010;

public class when_handle_is_async_with_await
{
    [Fact] async Task should_not_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    public record RegisterAuthor(string Name)
    {
        public async Task<string> Handle()
        {
            await Task.Delay(1);
            return Name;
        }
    }
}");
}
