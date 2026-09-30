// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Observability;
using Cratis.Traces;
using FluentValidation;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_validating;

public class and_it_is_traced : given.a_model_graph_validator
{
    public record RegisterAuthor(string Name);

    public class RegisterAuthorValidator : AbstractValidator<RegisterAuthor>
    {
        public RegisterAuthorValidator() => RuleFor(_ => _.Name).Must(_ => false).WithMessage("rejected");
    }

    IServiceProvider _serviceProvider;
    ActivitySource _source;
    TelemetryRecorder _telemetry;

    void Establish()
    {
        WithValidatorFor(typeof(RegisterAuthor), new RegisterAuthorValidator());
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

    async Task Because() => await _validator.Validate(new ModelGraphValidationRequest(new RegisterAuthor("a name"), _serviceProvider));

    Activity ValidatorSpan => _telemetry.Span("cratis.arc.validator.invoke");

    [Fact] void should_name_the_span_after_the_validator() => ValidatorSpan.DisplayName.ShouldEqual($"validate {nameof(RegisterAuthorValidator)}");
    [Fact] void should_add_the_validator_type() => ValidatorSpan.GetTagItem("cratis.arc.validator.type").ShouldEqual(typeof(RegisterAuthorValidator).FullName);
    [Fact] void should_add_the_number_of_results() => ValidatorSpan.GetTagItem("cratis.arc.validation.result_count").ShouldEqual(1);
}
