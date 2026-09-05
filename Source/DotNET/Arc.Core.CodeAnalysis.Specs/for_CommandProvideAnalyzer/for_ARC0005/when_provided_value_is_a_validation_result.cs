// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandProvideAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandProvideAnalyzer.for_ARC0005;

public class when_provided_value_is_a_validation_result
{
    [Fact] async Task should_not_report_diagnostic() => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace TestNamespace
{
    [Command]
    public record CreateOrder
    {
        public ValidationResult Provide() => ValidationResult.Error(""Not allowed"");
        public void Handle() { }
    }
}");
}
