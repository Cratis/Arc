// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.ModelBoundRecordAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_ModelBoundRecordAnalyzer;

public class when_command_is_a_class
{
    [Fact] async Task should_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    public class {|#0:RegisterAuthor|}
    {
        public string Name { get; set; }
    }
}",
        VerifyCS.Diagnostic("ARC0007")
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("RegisterAuthor"));
}
