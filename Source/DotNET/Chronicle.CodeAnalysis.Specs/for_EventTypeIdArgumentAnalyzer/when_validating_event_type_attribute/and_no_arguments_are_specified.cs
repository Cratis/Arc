// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.EventTypeIdArgumentAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_EventTypeIdArgumentAnalyzer.when_validating_event_type_attribute;

public class and_no_arguments_are_specified : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using Cratis.Chronicle.Events;

namespace TestNamespace
{
    [EventType]
    public record AuthorRegistered(string Name);
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
