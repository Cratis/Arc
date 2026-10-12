// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.when_analyzing;

public class with_computed_strings : given.a_wrapper_initializer
{
    [Fact] async Task should_not_guess_a_computed_source() => await VerifyCS.VerifyAnalyzerAsync(Source("""{ EventSourceType = "Account".ToString(), EventStreamType = "Typo" }"""));
    [Fact] async Task should_not_guess_a_computed_stream() => await VerifyCS.VerifyAnalyzerAsync(Source("""{ EventSourceType = "Account", EventStreamType = "Typo".ToString() }"""));
    [Fact] async Task should_not_guess_a_computed_typed_stream() => await VerifyCS.VerifyAnalyzerAsync(Source("""{ EventSource = typeof(AccountEventSource), EventStream = "Typo".ToString() }"""));
}
