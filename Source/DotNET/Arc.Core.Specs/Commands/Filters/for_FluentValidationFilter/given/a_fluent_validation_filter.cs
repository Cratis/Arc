// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Execution;
using FluentValidation.Results;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.given;

public class a_fluent_validation_filter : Specification
{
    protected IDiscoverableValidators _discoverableValidators;
    protected FluentValidationFilter _filter;
    protected CommandContext _context;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _discoverableValidators = Substitute.For<IDiscoverableValidators>();
        _filter = new FluentValidationFilter(_discoverableValidators);
        _context = new CommandContext(_correlationId, typeof(object), new object(), [], new());
    }

    protected static FluentValidation.Results.ValidationResult CreateValidationResult(bool isValid, params ValidationFailure[] errors)
    {
        return new FluentValidation.Results.ValidationResult(errors);
    }

    protected static ValidationFailure CreateValidationFailure(string propertyName, string errorMessage)
    {
        return new ValidationFailure(propertyName, errorMessage);
    }
}