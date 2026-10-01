// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Observability;
using Cratis.Traces;
using FluentValidation;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_validating;

/// <summary>
/// A validation failure thrown while a validator runs is invalid client input, so it must not fail the validator span.
/// </summary>
/// <remarks>
/// The invoker Arc ships turns most exceptions into a validation result, so the failure is thrown from the invoker
/// here to reach the span.
/// </remarks>
public class and_it_is_traced_and_a_validator_throws_a_validation_failure : given.a_model_graph_validator
{
    public record RegisterAuthor(string Name);

    public class RegisterAuthorValidator : AbstractValidator<RegisterAuthor>
    {
        public RegisterAuthorValidator() => RuleFor(_ => _.Name).NotEmpty();
    }

    IServiceProvider _serviceProvider;
    ActivitySource _source;
    Exception? _error;
    TelemetryRecorder _telemetry;

    void Establish()
    {
        var invoker = Substitute.For<IValidatorInvoker>();
        invoker
            .Invoke(Arg.Any<object>(), Arg.Any<IValidator>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<IEnumerable<ValidationResult>>>(_ => throw new TheValidationFailure());
        _validator = new ModelGraphValidator(_discoverableValidators, invoker);
        _discoverableValidators
            .TryGet(typeof(RegisterAuthor), Arg.Any<IServiceProvider>(), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[2] = new RegisterAuthorValidator();
                return true;
            });

        _source = new ActivitySource("Cratis.Arc.Test");
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

    async Task Because() => _error = await Catch.Exception(() => _validator.Validate(new ModelGraphValidationRequest(new RegisterAuthor("a name"), _serviceProvider)));

    Activity ValidatorSpan => _telemetry.Span(WellKnownTelemetryNames.ValidatorInvokeSpan);

    [Fact] void should_let_the_failure_through() => (_error is TheValidationFailure).ShouldBeTrue();
    [Fact] void should_leave_the_status_unset() => ValidatorSpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_record_the_exception() => ValidatorSpan.Events.Count(_ => _.Name == WellKnownTelemetryNames.ExceptionEvent).ShouldEqual(1);

    public class TheValidationFailure : Exception, IValidationFailure
    {
        public ValidationResult ValidationResult { get; } = ValidationResult.Error("missing identifier");
    }
}
