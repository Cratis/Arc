// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;
using static Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventStreamIdAnalyzer.given.stream_id_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventStreamIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventStreamIdAnalyzer;

public class when_the_stream_id_is_declared_twice
{
    const string Provider = "public EventStreamId GetEventStreamId() => \"x\";";
    static readonly string[] _arguments = ["C", "ICanProvideEventStreamId"];

    [Theory]
    [InlineData("constant")]
    [InlineData("{Name}")]
    public async Task should_report_an_attribute_value_beside_the_provider(string value) =>
        await VerifyCS.VerifyAnalyzerAsync(
            Command("{|#0:[EventStreamId(\"" + value + "\")]|}", Provider, " : ICanProvideEventStreamId"),
            new ExpectedDiagnostic("ARCCHR0018", DiagnosticSeverity.Error, _arguments[0], _arguments[1]));

    [Fact]
    public async Task should_not_report_the_provider_alone() =>
        await VerifyCS.VerifyAnalyzerAsync(Command("", Provider, " : ICanProvideEventStreamId"));

    [Fact]
    public async Task should_not_report_an_attribute_without_a_value_beside_the_provider() =>
        await VerifyCS.VerifyAnalyzerAsync(Command("[EventStreamId]", Provider, " : ICanProvideEventStreamId"));

    [Fact]
    public async Task should_not_report_the_attribute_alone() =>
        await VerifyCS.VerifyAnalyzerAsync(Command("[EventStreamId(\"constant\")]"));
}
