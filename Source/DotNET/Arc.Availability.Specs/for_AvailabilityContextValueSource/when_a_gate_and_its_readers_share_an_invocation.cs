// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Contexts;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Availability.for_AvailabilityContextValueSource;

public class when_a_gate_and_its_readers_share_an_invocation : given.an_invocation_host
{
    object _handler;
    object _copy;
    object _metadata;
    object _otherOperation;
    object _restored;

    async Task Because()
    {
        var context = new CommandContext(CorrelationId.New(), typeof(UngatedCommand), new { VesselId = "allowed" }, [], new CommandContextValues(), ServiceProvider: _scope.ServiceProvider)
        {
            ReceivedAt = Receipt
        };
        using (OperationContextScope.Begin(_scope.ServiceProvider))
        {
            await new Commands.Filters.AvailabilityFilter().OnExecution(context);
            _handler = await context.ProvidedValues.GetAsync("selected");
            _copy = await (context with { Dependencies = [new object()] }).ProvidedValues.GetAsync("selected");
            _metadata = (await _scope.ServiceProvider.GetRequiredService<IContextValueSource>().Read(typeof(UngatedCommand), "selected", CancellationToken.None)).Value;
            using (OperationContextScope.Begin(_scope.ServiceProvider))
            {
                var other = context with { Command = new { VesselId = "denied" } };
                _otherOperation = await other.ProvidedValues.GetAsync("selected");
            }

            _restored = await context.ProvidedValues.GetAsync("selected");
        }
    }

    [Fact] void should_read_the_gate_value_in_the_handler() => _handler.ShouldEqual("allowed");
    [Fact] void should_keep_the_value_through_dependency_binding() => _copy.ShouldEqual("allowed");
    [Fact] void should_not_guess_an_instance_for_a_type_only_reader() => _metadata.ShouldBeNull();
    [Fact] void should_not_share_the_value_with_a_nested_invocation_at_the_same_receipt() => _otherOperation.ShouldEqual("denied");
    [Fact] void should_keep_the_outer_value_after_a_nested_invocation() => _restored.ShouldEqual("allowed");
    [Fact] void should_resolve_once_per_invocation_and_once_for_metadata() => _calls.ShouldEqual(3);
}
