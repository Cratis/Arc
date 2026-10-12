// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.when_analyzing;

public class with_an_unknown_source_name : given.a_wrapper_initializer
{
    [Fact] async Task should_leave_legitimate_legacy_routing_alone() => await VerifyCS.VerifyAnalyzerAsync(Source("""{ EventSourceType = "Unknown", EventStreamType = "Typo" }"""));
}
