// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandAnalyzer;

public class when_handle_method_for_command_is_on_different_type
{
    [Fact] async Task should_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;

namespace TestNamespace
{
    [Command]
    public record CreateOrder(string OrderId)
    {
        public void Handle()
        {
        }
    }

    public class CreateOrderHandler
    {
        public void {|#0:Handle|}(CreateOrder command)
        {
        }
    }
}",
        VerifyCS.Diagnostic("ARC0003")
            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .WithLocation(0)
            .WithArguments("CreateOrderHandler", "CreateOrder"));
}
