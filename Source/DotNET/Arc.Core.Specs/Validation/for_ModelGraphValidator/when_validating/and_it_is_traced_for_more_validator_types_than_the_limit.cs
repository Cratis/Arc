// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Observability;
using Cratis.Traces;
using FluentValidation;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_validating;

/// <summary>
/// What a trace holds is bounded by the model, not by the payload: past the limit, validators run without a span.
/// </summary>
public class and_it_is_traced_for_more_validator_types_than_the_limit : given.a_model_graph_validator
{
    public record Item<T>(string Name);

    public record Marker1;
    public record Marker2;
    public record Marker3;
    public record Marker4;
    public record Marker5;
    public record Marker6;
    public record Marker7;
    public record Marker8;
    public record Marker9;
    public record Marker10;
    public record Marker11;
    public record Marker12;
    public record Marker13;
    public record Marker14;
    public record Marker15;
    public record Marker16;
    public record Marker17;
    public record Marker18;
    public record Marker19;
    public record Marker20;

    public record RegisterItems(IEnumerable<object> Items);

    static readonly Type[] _markers =
    [
        typeof(Marker1), typeof(Marker2), typeof(Marker3), typeof(Marker4), typeof(Marker5),
        typeof(Marker6), typeof(Marker7), typeof(Marker8), typeof(Marker9), typeof(Marker10),
        typeof(Marker11), typeof(Marker12), typeof(Marker13), typeof(Marker14), typeof(Marker15),
        typeof(Marker16), typeof(Marker17), typeof(Marker18), typeof(Marker19), typeof(Marker20)
    ];

    IServiceProvider _serviceProvider;
    System.Diagnostics.ActivitySource _source;
    TelemetryRecorder _telemetry;
    int _validatorTypes;

    void Establish()
    {
        // Each closed Item<T> is a distinct type, so each is validated by a validator of its own type.
        foreach (var marker in _markers)
        {
            var itemType = typeof(Item<>).MakeGenericType(marker);
            var validator = (IValidator)Activator.CreateInstance(typeof(ItemValidator<>).MakeGenericType(marker))!;
            _discoverableValidators
                .TryGet(itemType, Arg.Any<IServiceProvider>(), out Arg.Any<IValidator>())
                .Returns(x =>
                {
                    x[2] = validator;
                    return true;
                });
        }

        _source = new System.Diagnostics.ActivitySource("Cratis.Arc.Test");
        var activitySource = Substitute.For<IActivitySource<ModelGraphValidator>>();
        activitySource.ActualSource.Returns(_source);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(IActivitySource<ModelGraphValidator>)).Returns(activitySource);
        _telemetry = new TelemetryRecorder(_source);
    }

    void Destroy()
    {
        _telemetry.Dispose();
        _source.Dispose();
    }

    async Task Because()
    {
        var items = _markers.Select(_ => Activator.CreateInstance(typeof(Item<>).MakeGenericType(_), "an item")!).ToArray();
        _validatorTypes = items.Select(_ => _.GetType()).Distinct().Count();
        await _validator.Validate(new ModelGraphValidationRequest(new RegisterItems(items), _serviceProvider));
    }

    [Fact] void should_validate_more_types_than_the_limit() => _validatorTypes.ShouldBeGreaterThan(ValidatorSpans.MaxSpans);
    [Fact] void should_raise_no_more_validator_spans_than_the_limit() => _telemetry.Activities.Count(_ => _.OperationName == WellKnownTelemetryNames.ValidatorInvokeSpan).ShouldEqual(ValidatorSpans.MaxSpans);

    public class ItemValidator<T> : AbstractValidator<Item<T>>
    {
        public ItemValidator() => RuleFor(_ => _.Name).NotEmpty();
    }
}
