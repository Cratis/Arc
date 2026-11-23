// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Arc.Contexts;
using Cratis.Arc.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Queries.for_QueryPipeline.given;

public class a_query_with_bound_context_values : a_query_pipeline
{
    protected readonly DateTimeOffset _receipt = new(2026, 3, 4, 5, 6, 7, TimeSpan.Zero);
    protected readonly List<AvailabilityInvocation> _reads = [];
    protected AvailabilityInvocation _gated;
    protected QueryResult _result;
    protected object _validatedValue;
    protected object _performedValue;
    protected object _executionSnapshot;
    protected object _outerSnapshot;
    protected bool _outerValuesUnchanged;
    protected bool _outerSnapshotRestored;

    void Establish()
    {
        var source = Substitute.For<IExecutionContextValueSource>();
        source.ReadForExecution(Arg.Any<AvailabilityInvocation>(), "value", Arg.Any<CancellationToken>()).Returns(call =>
        {
            _reads.Add(call.Arg<AvailabilityInvocation>());
            return ContextValueRead.Present("bound");
        });
        var gate = Substitute.For<IAvailabilityExecutionGate>();
        gate.IsGated(typeof(object)).Returns(true);
        gate.Decide(Arg.Any<AvailabilityInvocation>(), _serviceProvider, Arg.Any<CancellationToken>()).Returns(call =>
        {
            _gated = call.Arg<AvailabilityInvocation>();
            return AvailabilityDecision.Available;
        });
        var clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_receipt);
        _serviceProvider.GetService(typeof(TimeProvider)).Returns(clock);
        _serviceProvider.GetService(typeof(IAvailabilityGate)).Returns(gate);
        _serviceProvider.GetService(typeof(IContextValueSource)).Returns(source);
        _serviceProvider.GetService(typeof(IQueryPerformerProviders)).Returns(_queryPerformerProviders);
        _queryPerformer.Parameters.Returns(new QueryParameters([new QueryParameter("count", typeof(int))]));
        _queryPerformer.Dependencies.Returns([]);
        _queryPerformerProviders.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out var _).Returns(call =>
        {
            call[1] = _queryPerformer;
            return true;
        });
        var validator = Substitute.For<IValidator>();
        validator.ValidateAsync(Arg.Any<IValidationContext>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            _validatedValue = await call.Arg<IValidationContext>().ProvidedValues().GetAsync("value");
            return new FluentValidation.Results.ValidationResult();
        });
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(async _ =>
        {
            var snapshot = OperationSnapshot.Current!;
            _executionSnapshot = snapshot;
            await new ValidatorInvoker(NullLogger<ValidatorInvoker>.Instance).Invoke(42, validator, "count");
            _performedValue = await (InvocationContextValues.For(snapshot) ?? ContextValues.Empty).GetAsync("value");
            return null;
        });
    }

    protected async Task PerformInEnclosingOperation()
    {
        using var outer = OperationSnapshot.Begin(_serviceProvider, () => null);
        _outerSnapshot = outer;
        InvocationContextValues.Attach(outer, ContextValues.Empty);
        _result = await _pipeline.Perform("Test.Count", new QueryArguments { ["count"] = "42" }, Paging.NotPaged, Sorting.None, _serviceProvider);
        _outerValuesUnchanged = ReferenceEquals(InvocationContextValues.For(outer), ContextValues.Empty);
        _outerSnapshotRestored = ReferenceEquals(OperationSnapshot.Current, outer);
    }
}
