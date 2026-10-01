// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Observability;
using Cratis.Traces;
using FluentValidation;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_validating;

/// <summary>
/// A validator runs for every item of a collection, but the trace holds one span for it, not one per item.
/// </summary>
public class and_it_is_traced_for_a_collection : given.a_model_graph_validator
{
    public record Author(string Name);

    public record RegisterAuthors(IEnumerable<Author> Authors);

    public class AuthorValidator : AbstractValidator<Author>
    {
        public AuthorValidator() => RuleFor(_ => _.Name).NotEmpty();
    }

    IServiceProvider _serviceProvider;
    System.Diagnostics.ActivitySource _source;
    TelemetryRecorder _telemetry;

    void Establish()
    {
        _discoverableValidators
            .TryGet(typeof(Author), Arg.Any<IServiceProvider>(), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[2] = new AuthorValidator();
                return true;
            });

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

    async Task Because() => await _validator.Validate(new ModelGraphValidationRequest(
        new RegisterAuthors([.. Enumerable.Range(0, 50).Select(_ => new Author($"author {_}"))]),
        _serviceProvider));

    [Fact] void should_raise_one_span_for_the_validator() => _telemetry.Activities.Count(_ => _.OperationName == WellKnownTelemetryNames.ValidatorInvokeSpan).ShouldEqual(1);
}
