// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.when_analyzing;

public class with_typed_alternative_declared_streams : given.a_wrapper_initializer
{
    [Fact] async Task should_not_report_a_different_declared_stream() => await VerifyCS.VerifyAnalyzerAsync(Source("""{ EventSource = typeof(AccountEventSource), EventStream = "Adjustments" }"""));
    [Fact] async Task should_not_report_a_different_stream_of_the_command_definition() => await VerifyCS.VerifyAnalyzerAsync(Source("""{ EventStream = "Adjustments" }"""));
}
