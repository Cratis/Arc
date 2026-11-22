// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Arc.Validation;

/// <summary>
/// Represents a <see cref="IModelValidator"/> for <see cref="BaseValidator{T}"/>.
/// </summary>
public class DiscoverableModelValidator : IModelValidator
{
    readonly IValidator? _validator;
    readonly Type? _modelType;
    readonly IDiscoverableValidators? _discoverableValidators;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverableModelValidator"/> class.
    /// </summary>
    /// <param name="validator">The <see cref="IValidator"/> to use.</param>
    public DiscoverableModelValidator(IValidator validator)
    {
        _validator = validator;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoverableModelValidator"/> class that resolves from the request's services.
    /// </summary>
    /// <param name="modelType">Type of model to get a validator for.</param>
    /// <param name="discoverableValidators">The <see cref="IDiscoverableValidators"/> to use for looking up validators.</param>
    public DiscoverableModelValidator(Type modelType, IDiscoverableValidators discoverableValidators)
    {
        _modelType = modelType;
        _discoverableValidators = discoverableValidators;
    }

    /// <inheritdoc/>
    /// <exception cref="DiscoverableModelValidatorNotFound">The validator is no longer available during validation.</exception>
    public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
    {
        var failures = new List<ModelValidationResult>();
        if (context.Model is null)
        {
            return failures;
        }

        var validator = _validator ?? ResolveValidator(context);
        if (ShouldSkipConceptValidation(context, validator))
        {
            return failures;
        }

        // A ValidationContext<object> is enough: AbstractValidator<T>.ValidateAsync rebuilds a ValidationContext<T>
        // from any non-generic context whose instance is a T, keeping its root context data (command or query).
        // Building ValidationContext<T> through MakeGenericType is not something NativeAOT or trimming can preserve.
        var validationContext = new ValidationContext<object>(context.Model);

        SetValidationType(context, validationContext);

        var result = validator.ValidateAsync(validationContext).GetAwaiter().GetResult();
        failures.AddRange(result.Errors.Select(x => new ModelValidationResult(x.PropertyName, x.ErrorMessage)));
        return failures;
    }

    static bool IsCommandMethod(string method) =>
        HttpMethods.IsPost(method) ||
        HttpMethods.IsPut(method) ||
        HttpMethods.IsPatch(method) ||
        HttpMethods.IsDelete(method);

    IValidator ResolveValidator(ModelValidationContext context)
    {
        try
        {
            return _discoverableValidators!.TryGet(_modelType!, context.ActionContext.HttpContext.RequestServices, out var validator)
                ? validator
                : throw new DiscoverableModelValidatorNotFound(_modelType!);
        }
        catch (NoCommandContextAvailable exception)
        {
            // MVC model validation runs during model binding, before the command context is established, so a
            // validator that depends on a command-scoped read model cannot be constructed here. Surface an
            // actionable error instead of the opaque dependency-resolution failure.
            throw new ReadModelValidatorRequiresCommandPipeline(_modelType!, exception);
        }
    }

    void SetValidationType(ModelValidationContext context, IValidationContext validationContext)
    {
        if (IsCommandMethod(context.ActionContext.HttpContext.Request.Method))
        {
            validationContext.SetCommand();
        }
        else
        {
            validationContext.SetQuery();
        }
    }

    bool ShouldSkipConceptValidation(ModelValidationContext context, IValidator validator)
    {
        var hasAspNetResult = context.ActionContext.ActionDescriptor.FilterDescriptors
            .Any(_ => _.Filter is AspNetResultAttribute);

        if (!hasAspNetResult)
        {
            return false;
        }

        var validatorType = validator.GetType();
        while (validatorType != typeof(object))
        {
            if (validatorType.IsGenericType &&
                validatorType.GetGenericTypeDefinition() == typeof(ConceptValidator<>))
            {
                return true;
            }
            validatorType = validatorType.BaseType!;
        }

        return false;
    }
}
