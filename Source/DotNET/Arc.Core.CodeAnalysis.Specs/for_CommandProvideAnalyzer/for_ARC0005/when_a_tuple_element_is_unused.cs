// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandProvideAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandProvideAnalyzer.for_ARC0005;

public class when_a_tuple_element_is_unused
{
    [Fact] async Task should_report_diagnostic_for_the_unused_element() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    public class UsedData { }
    public class UnusedData { }

    [Command]
    public record CreateOrder
    {
        public (UsedData Used, UnusedData Unused) {|#0:Provide|}() => (new UsedData(), new UnusedData());
        public void Handle(UsedData used) { }
    }
}",
        VerifyCS.Diagnostic("ARC0005")
            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("UnusedData", "CreateOrder"));
}
