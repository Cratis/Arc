// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandAnalyzer;

public class when_command_handle_method_is_not_public
{
    [Fact] async Task should_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    public class {|#0:CreateOrder|}
    {
        public string OrderId { get; set; }

        internal void Handle()
        {
        }
    }
}",
        VerifyCS.Diagnostic("ARC0004")
            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("CreateOrder"));
}
