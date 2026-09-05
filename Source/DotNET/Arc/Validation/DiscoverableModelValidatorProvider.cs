// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Arc.Validation;

/// <summary>
/// Represents a <see cref="IModelValidatorProvider"/> for <see cref="DiscoverableValidator{T}"/>.
/// </summary>
/// <param name="discoverableValidators">The <see cref="IDiscoverableValidators"/> to use for looking up validators.</param>
public class DiscoverableModelValidatorProvider(IDiscoverableValidators discoverableValidators) : IModelValidatorProvider
{
    /// <inheritdoc/>
    public void CreateValidators(ModelValidatorProviderContext context)
    {
        try
        {
            if (discoverableValidators.TryGet(context.ModelMetadata.ModelType, out var validator))
            {
                var modelValidator = new DiscoverableModelValidator(validator);
                context.Results.Add(new ValidatorItem
                {
                    IsReusable = false,
                    Validator = modelValidator
                });
            }
        }
        catch (NoCommandContextAvailable exception)
        {
            // MVC model validation runs during model binding, before the command context is established, so a
            // validator that depends on a command-scoped read model cannot be constructed here. Surface an
            // actionable error instead of the opaque dependency-resolution failure.
            throw new ReadModelValidatorRequiresCommandPipeline(context.ModelMetadata.ModelType, exception);
        }
    }
}
