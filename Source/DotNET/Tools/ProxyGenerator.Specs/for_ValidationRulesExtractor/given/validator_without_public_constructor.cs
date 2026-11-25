// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor.given;

public class validator_without_public_constructor : AbstractValidator<model_without_constructible_validator>
{
    validator_without_public_constructor()
    {
    }
}
