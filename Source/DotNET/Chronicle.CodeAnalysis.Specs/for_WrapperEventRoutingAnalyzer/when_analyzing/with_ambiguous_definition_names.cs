// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.when_analyzing;

public class with_ambiguous_definition_names : given.a_wrapper_initializer
{
    [Fact] async Task should_not_choose_a_definition() => await VerifyCS.VerifyAnalyzerAsync(Source("""{ EventSourceType = "Account", EventStreamType = "Typo" }""", "") + @"
[EventSource(""Account"")]
[EventStream(""Typo"")]
public class AnotherAccount : IEventSource;
");
}
