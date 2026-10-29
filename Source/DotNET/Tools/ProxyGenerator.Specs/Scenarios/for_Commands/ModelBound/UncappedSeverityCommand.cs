// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound;

[Command]
public record UncappedSeverityCommand(string Value)
{
    public static int Handled;
    public void Handle() => Interlocked.Increment(ref Handled);
}

public class UncappedSeverityCommandValidator : CommandValidator<UncappedSeverityCommand>
{
    public UncappedSeverityCommandValidator() => RuleFor(command => command.Value).Equal("ok").WithMessage("Value must be ok");
}

[Command]
[AllowCallerValidationSeverity(ValidationResultSeverity.Error)]
public record CappedSeverityCommand(string Value)
{
    public static int Handled;
    public void Handle() => Interlocked.Increment(ref Handled);
}

public class CappedSeverityCommandValidator : CommandValidator<CappedSeverityCommand>
{
    public CappedSeverityCommandValidator() => RuleFor(command => command.Value).Equal("ok").WithMessage("Value must be ok");
}
