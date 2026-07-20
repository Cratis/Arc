// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_validating;

/// <summary>
/// Severity is what lets a caller allow warnings through while still rejecting errors. Flattening everything to
/// Error would silently take that choice away, so the validator's own severity has to survive the traversal.
/// </summary>
public class and_the_validator_reports_a_warning : given.a_model_graph_validator
{
    IEnumerable<ValidationResult> _results;

    void Establish() => WithValidatorFor(typeof(Model), new ModelValidator());

    async Task Because() => _results = await _validator.Validate(new ModelGraphValidationRequest(new Model()));

    [Fact] void should_report_the_warning() => _results.Count().ShouldEqual(1);
    [Fact] void should_preserve_the_severity() => _results.Single().Severity.ShouldEqual(ValidationResultSeverity.Warning);

    record Model
    {
        public string Name { get; init; } = string.Empty;
    }

    class ModelValidator : AbstractValidator<Model>
    {
        public ModelValidator() => RuleFor(x => x.Name).NotEmpty().WithSeverity(Severity.Warning);
    }
}
