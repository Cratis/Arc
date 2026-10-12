// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.Chronicle.CodeAnalysis.WrapperEventRoutingAnalyzer>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_WrapperEventRoutingAnalyzer.when_analyzing;

public class with_a_route_value : given.a_wrapper_initializer
{
    [Fact] async Task should_not_report_route_dot_route() => await VerifyCS.VerifyAnalyzerAsync(Definitions + @"
[Command, Cratis.Arc.Chronicle.Commands.EventSource<AccountEventSource>(""Transactions"")]
public record Deposit
{
    public EventForEventSourceId Handle(IEventRoutes routes) =>
        routes.For<AccountEventSource>(""Adjustments"").Route(""id"", new object());
}
");
}
