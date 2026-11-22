// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidator.given;

// NSubstitute's class proxy supports the default interface members through these abstract implementations.
public abstract class DiscoverableValidatorsSubstitute : IDiscoverableValidators
{
    public abstract bool HasValidatorFor(Type modelType);
    public abstract bool TryGet(Type modelType, out IValidator validator);
    public abstract bool TryGet(Type modelType, IServiceProvider serviceProvider, out IValidator validator);
}
