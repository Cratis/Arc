// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.given;

/// <summary>
/// Gives every node a validator and records each invocation as "type@path", which pins the whole traversal: which
/// nodes it reaches, in which order, and under which member path.
/// </summary>
public class a_recording_model_graph_validator : Specification
{
    protected ModelGraphValidator _validator;
    protected List<string> _visits;

    /// <summary>
    /// Gets the recorded traversal as one string, in visiting order.
    /// </summary>
    protected string Traversal => string.Join(" | ", _visits);

    void Establish()
    {
        _visits = [];
        var discoverableValidators = Substitute.For<IDiscoverableValidators>();
        var validator = Substitute.For<IValidator>();
        discoverableValidators.TryGet(Arg.Any<Type>(), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = validator;
                return true;
            });

        var validatorInvoker = Substitute.For<IValidatorInvoker>();
        validatorInvoker.Invoke(default!, default!, default!, default)
            .ReturnsForAnyArgs(x =>
            {
                _visits.Add($"{x[0].GetType().Name}@{x[2]}");
                return Task.FromResult<IEnumerable<ValidationResult>>([]);
            });

        _validator = new ModelGraphValidator(discoverableValidators, validatorInvoker);
    }

    /// <summary>
    /// Validates a graph from its root.
    /// </summary>
    /// <param name="root">The root instance.</param>
    protected void Walk(object root) => _validator.Validate(new ModelGraphValidationRequest(root)).GetAwaiter().GetResult();
}
