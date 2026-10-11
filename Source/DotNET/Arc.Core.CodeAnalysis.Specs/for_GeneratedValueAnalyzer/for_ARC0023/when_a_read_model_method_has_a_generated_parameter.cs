// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0023;

public class when_a_read_model_method_has_a_generated_parameter : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("", "Handle")]
    [InlineData("static", "Handle")]
    [InlineData("static", "Query")]
    [InlineData("", "Other")]
    public async Task should_report_every_read_model_method(string modifier, string method) => await VerifyDiagnostic(
        $@"
        [Cratis.Arc.Queries.ModelBound.ReadModel]
        public record Model
        {{
            public {modifier} void {method}([{{|#0:GeneratedValue|}}] CartId value) {{ }}
        }}",
        "ARC0023",
        "value");

    [Fact] async Task should_report_methods_on_an_inherited_read_model_type() => await VerifyDiagnostic(
        @"
        [Cratis.Arc.Queries.ModelBound.ReadModel] public record Parent;
        public record Model : Parent
        {
            public void Handle([{|#0:GeneratedValue|}] CartId value) { }
        }",
        "ARC0023",
        "value");
}
