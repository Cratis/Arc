// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventStreamIdAnalyzer.given.stream_id_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventStreamIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventStreamIdAnalyzer;

public class when_validating_a_template
{
    [Theory]
    [InlineData("{Name}", "")]
    [InlineData("{Name}:{Count}", "public int Count { get; init; }")]
    [InlineData("{Value}", "public Period Value { get; init; } = default!;")]
    [InlineData("{Value}", "public ScopeId Value { get; init; } = default!;")]
    [InlineData("{Value}", "public EventSourceId Value { get; init; } = default!;")]
    [InlineData("{Value}", "public Kind Value { get; init; }")]
    [InlineData("{Value}", "public Guid Value { get; init; }")]
    [InlineData("{Value}", "public DateOnly Value { get; init; }")]
    [InlineData("{Value}", "public DateTimeOffset Value { get; init; }")]
    [InlineData("{Value}", "public decimal Value { get; init; }")]
    [InlineData("{Value}", "public int? Value { get; init; }")]
    [InlineData("{{{Name}", "")]
    [InlineData("{{Name}}", "")]
    [InlineData("prefix-{{}}-{Name}", "")]
    [InlineData("constant", "")]
    [InlineData("", "")]
    public async Task should_accept_a_valid_template(string template, string members) =>
        await VerifyCS.VerifyAnalyzerAsync(Command("[EventStreamId(\"" + template + "\")]", members));

    [Fact]
    public async Task should_ignore_an_attribute_without_a_value() =>
        await VerifyCS.VerifyAnalyzerAsync(Command("[EventStreamId]"));

    [Fact]
    public async Task should_ignore_a_type_that_is_not_a_command() =>
        await VerifyCS.VerifyAnalyzerAsync(Preamble + "\n[EventStreamId(\"{Missing}\")] public record C(string Name);");

    [Fact]
    public async Task should_report_an_unknown_property() =>
        await VerifyCS.VerifyAnalyzerAsync(Command("[EventStreamId({|#0:\"{Missing}\"|})]"), Invalid("{Missing}", "'Missing' is not a public instance property"));

    [Theory]
    [InlineData("private string Secret { get; init; } = \"\";")]
    [InlineData("internal string Secret { get; init; } = \"\";")]
    [InlineData("public static string Secret { get; } = \"\";")]
    public async Task should_report_a_property_that_is_not_public_and_instance(string members) =>
        await VerifyCS.VerifyAnalyzerAsync(Command("[EventStreamId({|#0:\"{Secret}\"|})]", members), Invalid("{Secret}", "'Secret' is not a public instance property"));

    [Theory]
    [InlineData("Complex")]
    [InlineData("object")]
    [InlineData("string[]")]
    public async Task should_report_a_property_that_does_not_convert_to_a_string(string type) =>
        await VerifyCS.VerifyAnalyzerAsync(
            Command("[EventStreamId({|#0:\"{Value}\"|})]", "public " + type + " Value { get; init; } = default!;"),
            Invalid("{Value}", "does not convert to a string part"));

    [Theory]
    [InlineData("{Name", "is not closed")]
    [InlineData("Name}", "was never opened")]
    [InlineData("{Name}}", "was never opened")]
    [InlineData("{Na{me}", "is not closed")]
    [InlineData("a{}b", "has no property name")]
    public async Task should_report_malformed_braces(string template, string problem) =>
        await VerifyCS.VerifyAnalyzerAsync(Command("[EventStreamId({|#0:\"" + template + "\"|})]"), Invalid(template, problem));

    [Fact]
    public async Task should_report_each_invalid_placeholder() =>
        await VerifyCS.VerifyAnalyzerAsync(
            Command("[EventStreamId(\"{A}:{B}\")]"),
            Invalid("{A}:{B}", "'A' is not a public instance property"),
            Invalid("{A}:{B}", "'B' is not a public instance property"));
}
