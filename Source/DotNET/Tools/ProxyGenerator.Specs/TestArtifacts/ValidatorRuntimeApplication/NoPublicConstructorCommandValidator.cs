// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace ValidatorRuntimeApplication;

public class NoPublicConstructorCommandValidator : CommandValidator<NoPublicConstructorCommand>
{
    NoPublicConstructorCommandValidator()
    {
    }
}
