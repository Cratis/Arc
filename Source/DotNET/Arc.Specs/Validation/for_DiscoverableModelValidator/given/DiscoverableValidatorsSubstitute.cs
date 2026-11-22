// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidator.given;

/// <summary>
/// Supports substituting default interface members through abstract implementations for NSubstitute's class proxy.
/// </summary>
public abstract class DiscoverableValidatorsSubstitute : IDiscoverableValidators
{
    public abstract bool HasValidatorFor(Type modelType);
    public abstract bool TryGet(Type modelType, out IValidator validator);
    public abstract bool TryGet(Type modelType, IServiceProvider serviceProvider, out IValidator validator);
}
