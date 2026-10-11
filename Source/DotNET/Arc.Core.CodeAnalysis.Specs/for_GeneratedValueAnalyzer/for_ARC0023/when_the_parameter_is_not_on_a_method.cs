// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0023;

public class when_the_parameter_is_not_on_a_method : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("[Command] public record Example([{|#0:GeneratedValue|}] CartId value);")]
    [InlineData("public delegate void Example([{|#0:GeneratedValue|}] CartId value);")]
    [InlineData("public class Example { public Example([{|#0:GeneratedValue|}] CartId value) { } }")]
    [InlineData("public class Example { public CartId this[[{|#0:GeneratedValue|}] CartId value] => value; }")]
    [InlineData("[Command] public record Example { public void Handle() { void Local([{|#0:GeneratedValue|}] CartId value) { } } }")]
    [InlineData("[Command] public record Example { public void Handle() { Action<CartId> create = ([{|#0:GeneratedValue|}] CartId value) => { }; } }")]
    public async Task should_report_unsupported_parameter_declarations(string declaration) => await VerifyDiagnostic(declaration, "ARC0023", "value");
}
