// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Observability;
using Cratis.Traces;
using FluentValidation;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_validating;

public class and_it_is_traced_and_cancelled : given.a_model_graph_validator
{
    public record RegisterAuthor(string Name);

    public class RegisterAuthorValidator : AbstractValidator<RegisterAuthor>
    {
        public RegisterAuthorValidator() => RuleFor(_ => _.Name).Must(_ => throw new OperationCanceledException());
    }

    IServiceProvider _serviceProvider;
    CancellationTokenSource _cancellation;
    ActivitySource _source;
    Exception? _error;
    TelemetryRecorder _telemetry;

    void Establish()
    {
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
        _cancellation = new();
        _cancellation.Cancel();
    }

    void Destroy()
    {
        _cancellation.Dispose();
        _telemetry.Dispose();
        _source.Dispose();
    }

    async Task Because() => _error = await Catch.Exception(() => _validator.Validate(new ModelGraphValidationRequest(new RegisterAuthor("a name"), _serviceProvider), _cancellation.Token));

    Activity ValidatorSpan => _telemetry.Span(WellKnownTelemetryNames.ValidatorInvokeSpan);

    [Fact] void should_let_the_cancellation_through() => (_error is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_leave_the_status_unset() => ValidatorSpan.Status.ShouldEqual(ActivityStatusCode.Unset);
    [Fact] void should_record_the_exception() => ValidatorSpan.Events.Count(_ => _.Name == WellKnownTelemetryNames.ExceptionEvent).ShouldEqual(1);
}
