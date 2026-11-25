// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET11_0_OR_GREATER
using Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;
using static Cratis.Arc.Chronicle.CodeAnalysis.for_CommandConcurrencyAttributeAnalyzer.given.concurrency_source;
using static Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.NativeUnionSource;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.for_CommandConcurrencyAttributeAnalyzer;

public class when_returning_native_union_scopes : Specification
{
    [Theory]
    [InlineData("Scopes")]
    [InlineData("Single")]
    [InlineData("Task<Scopes>")]
    [InlineData("(int, (string, Scopes))")]
    [InlineData("Choice<Scopes, int>")]
    public Task should_report_ignored_blanket_concurrency(string signature) => Verify<CommandConcurrencyAttributeAnalyzer>(
        Command("[EventStreamId(\"id\", concurrency: true)]", signature) +
        "public union Scopes(EventsWithConcurrencyScopes, string); public union Single(EventsWithConcurrencyScopes); public union Choice<T,U>(T,U);",
        Warning("EventStreamId"));
}
#endif
