// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound;

[Command]
[BlockOnValidationSeverity(ValidationResultSeverity.Information)]
public record SeverityPolicyCommand(string Level)
{
    public static int Handled;
    public void Handle() => Interlocked.Increment(ref Handled);
}

public class SeverityPolicyCommandValidator : CommandValidator<SeverityPolicyCommand>
{
    public SeverityPolicyCommandValidator()
    {
        RuleFor(command => command.Level).Equal("ok").WithMessage("Keep warning message")
            .WithSeverity(Severity.Warning).When(command => command.Level == "warning");
        RuleFor(command => command.Level).Equal("ok").WithMessage("Keep information message")
            .WithSeverity(Severity.Info).When(command => command.Level == "information");
    }
}
