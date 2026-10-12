// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.when_analyzing;

public class with_sentinel_values : given.a_wrapper_initializer
{
    [Fact] async Task should_not_report_named_sentinels() => await VerifyCS.VerifyAnalyzerAsync(Source("{ EventSourceType = EventSourceType.Default, EventStreamType = EventStreamType.All, EventStreamId = EventStreamId.Default }"));
    [Fact] async Task should_not_report_literal_sentinels() => await VerifyCS.VerifyAnalyzerAsync(Source("""{ EventSourceType = "Default", EventStreamType = "All", EventStreamId = "free-form" }"""));
}
