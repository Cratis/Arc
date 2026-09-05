// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.InjectedReadModelAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_InjectedReadModelAnalyzer.for_ARC0006;

public class when_handle_parameter_is_non_nullable_read_model
{
    [Fact] async Task should_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;

namespace TestNamespace
{
    [Command]
    public record AssignContact
    {
        public void Handle({|#0:Customer customer|}) { }
    }

    [ReadModel]
    public class Customer { }
}",
        VerifyCS.Diagnostic("ARC0006")
            .WithSeverity(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
            .WithLocation(0)
            .WithArguments("Customer", "AssignContact.Handle"));
}
