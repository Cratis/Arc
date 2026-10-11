// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0023;

public class when_the_parameter_is_outside_command_handle : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("[Command]", "public void Provide([{|#0:GeneratedValue|}] CartId value) { }")]
    [InlineData("[Command]", "private static void Provide([{|#0:GeneratedValue|}] CartId value) { }")]
    [InlineData("[Command]", "public static void Handle([{|#0:GeneratedValue|}] CartId value) { }")]
    [InlineData("[Command]", "public void Other([{|#0:GeneratedValue|}] CartId value) { }")]
    [InlineData("", "public void Handle([{|#0:GeneratedValue|}] CartId value) { }")]
    [InlineData("[Cratis.Arc.Queries.ModelBound.ReadModel]", "public static Example Query([{|#0:GeneratedValue|}] CartId value) => new();")]
    [InlineData("", "public void Action([{|#0:GeneratedValue|}] CartId value) { }")]
    public async Task should_report_unsupported_method_parameters(string attribute, string method) => await VerifyDiagnostic(
        $@"
        {attribute}
        public sealed record Example
        {{
            {method}
        }}",
        "ARC0023",
        "value");
}
