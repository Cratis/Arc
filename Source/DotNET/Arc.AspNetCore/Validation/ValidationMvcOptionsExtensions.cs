// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for working with <see cref="MvcOptions"/>.
/// </summary>
public static class ValidationMvcOptionsExtensions
{
    /// <summary>
    /// Add CQRS setup.
    /// </summary>
    /// <param name="options"><see cref="MvcOptions"/> to build on.</param>
    /// <param name="discoverableValidators"><see cref="IDiscoverableValidators"/> for looking up validators.</param>
    /// <returns><see cref="MvcOptions"/> for building continuation.</returns>
    public static MvcOptions AddValidation(this MvcOptions options, IDiscoverableValidators discoverableValidators)
    {
        options.ModelValidatorProviders.Add(new DiscoverableModelValidatorProvider(discoverableValidators));
        return options;
    }
}
