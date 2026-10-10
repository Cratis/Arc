// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventStreamIdAnalyzer.given.stream_id_source;
using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventStreamIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventStreamIdAnalyzer;

public class when_the_stream_id_is_provided_dynamically
{
    const string Provider = "public EventStreamId GetEventStreamId() => \"x\";";

    [Theory]
    [InlineData("[EventStreamId(\"\")]")]
    [InlineData("[EventStreamId(null)]")]
    [InlineData("[EventStreamId]")]
    public async Task should_not_report_an_attribute_without_a_stream_id_beside_the_provider(string attribute) =>
        await VerifyCS.VerifyAnalyzerAsync(Command(attribute, Provider, " : ICanProvideEventStreamId"));

    [Theory]
    [InlineData("[EventStreamId(\"\")]")]
    [InlineData("[EventStreamId(null)]")]
    public async Task should_not_report_an_empty_or_null_attribute_alone(string attribute) =>
        await VerifyCS.VerifyAnalyzerAsync(Command(attribute));

    [Fact]
    public async Task should_not_report_the_provider_alone() =>
        await VerifyCS.VerifyAnalyzerAsync(Command("", Provider, " : ICanProvideEventStreamId"));
}
