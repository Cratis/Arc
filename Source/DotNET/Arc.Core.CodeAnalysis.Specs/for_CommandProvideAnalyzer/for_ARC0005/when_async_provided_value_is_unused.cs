// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandProvideAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandProvideAnalyzer.for_ARC0005;

public class when_async_provided_value_is_unused
{
    [Fact] async Task should_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using System.Threading.Tasks;
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    public class ExtraData { }

    [Command]
    public record CreateOrder
    {
        public Task<ExtraData> {|#0:Provide|}() => Task.FromResult(new ExtraData());
        public void Handle() { }
    }
}",
        VerifyCS.Diagnostic("ARC0005")
            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("ExtraData", "CreateOrder"));
}
