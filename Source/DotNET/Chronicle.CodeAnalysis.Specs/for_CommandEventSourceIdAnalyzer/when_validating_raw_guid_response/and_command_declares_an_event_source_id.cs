// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.CommandEventSourceIdAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_CommandEventSourceIdAnalyzer.when_validating_raw_guid_response;

public class and_command_declares_an_event_source_id : Specification
{
    Exception _result;

    async Task Because() => _result = await Catch.Exception(async () => await VerifyCS.VerifyAnalyzerAsync(@"
using System;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace TestNamespace
{
    [EventType]
    public record ItemAddedToCart(string Sku);

    [Command]
    public record AddItemToCart([Key] Guid CartId, string Sku)
    {
        public (Guid, ItemAddedToCart) Handle() => (Guid.NewGuid(), new ItemAddedToCart(Sku));
    }
}"));

    [Fact] void should_not_report_diagnostic() => _result.ShouldBeNull();
}
