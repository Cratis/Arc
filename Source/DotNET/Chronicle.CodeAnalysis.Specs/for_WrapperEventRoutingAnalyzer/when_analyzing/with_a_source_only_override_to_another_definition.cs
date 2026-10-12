// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.when_analyzing;

public class with_a_source_only_override_to_another_definition : given.a_wrapper_initializer
{
    const string LedgerDefinition = """[EventSource, EventStream("Entries")] public class LedgerEventSource : IEventSource;""";

    [Fact]
    async Task should_not_warn_for_an_inherited_stream() => await VerifyCS.VerifyAnalyzerAsync(Source("""{ EventSourceType = "Ledger" }""") + LedgerDefinition);
    [Fact]
    async Task should_not_warn_for_a_named_stream_sentinel() => await VerifyCS.VerifyAnalyzerAsync(Source("""{ EventSourceType = "Ledger", EventStreamType = EventStreamType.All }""") + LedgerDefinition);
    [Fact]
    async Task should_not_warn_for_a_literal_stream_sentinel() => await VerifyCS.VerifyAnalyzerAsync(Source("""{ EventSourceType = "Ledger", EventStreamType = "All" }""") + LedgerDefinition);
}
