// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using FluentValidation;

namespace Cratis.Arc.Testing.for_CommandScenario;

/// <summary>
/// Requires a <see cref="NamedWork"/> to carry a name.
/// </summary>
public class NamedWorkValidator : CommandValidator<NamedWork>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NamedWorkValidator"/> class.
    /// </summary>
    public NamedWorkValidator() => RuleFor(_ => _.Name).NotEmpty();
}
