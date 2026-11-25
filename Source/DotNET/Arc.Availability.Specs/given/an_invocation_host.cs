// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.given;

public class an_invocation_host : an_availability_host
{
    protected static readonly DateTimeOffset Receipt = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
    protected int _calls;
    protected override ContextValueCatalog ValueCatalog => new ContextValueCatalog()
        .Declare(new ContextValueDeclaration("selected", new CodeContextValueProvider(ReadSelected), ContextScope.Application, Required: false));

    protected override void Configure(ArcAvailabilityOptions options) => options.Gate<UngatedCommand>(AvailabilityExpression.When(
        new ContextComparison("$context.selected", ComparisonOperator.Equal, "allowed")));

    protected virtual async ValueTask<object?> ReadSelected(IContextValueResolver resolver, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        var value = await resolver.Resolve("arguments.vesselId", cancellationToken);
        return value.IsPresent ? value.Value : null;
    }

    protected ValueTask<AvailabilityDecision> Decide(object? arguments, DateTimeOffset? receipt = null) =>
        ((IAvailabilityExecutionGate)_gate).Decide(new AvailabilityInvocation(typeof(UngatedCommand), arguments, receipt ?? Receipt), _scope.ServiceProvider, CancellationToken.None);
}
