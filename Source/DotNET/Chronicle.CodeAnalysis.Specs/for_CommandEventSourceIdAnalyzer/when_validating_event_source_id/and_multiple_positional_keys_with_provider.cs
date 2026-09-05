// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_event_source_id;

public class and_multiple_positional_keys_with_provider : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace TestNamespace
{
    [Command]
    public record Change([Key] Guid First, [Key] Guid Second) : ICanProvideEventSourceId
    {
        public EventSourceId GetEventSourceId() => First.ToString();
        public void Handle() { }
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
