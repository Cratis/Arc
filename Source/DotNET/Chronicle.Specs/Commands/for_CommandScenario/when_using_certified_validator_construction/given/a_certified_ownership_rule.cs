// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.DependencyInjection;
using FluentValidation;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_certified_validator_construction.given;

public class a_certified_ownership_rule : a_certified_command
{
    protected int _calls;

    void Establish()
    {
        _scenario.Services.AddProtectedDecisionValidator<CertifiedValidator>(validator =>
        {
            _calls++;
            validator.RuleFor(command => command.EventSourceId).Must(_ => false).WithMessage("ownership denied");
        });
        Build();
    }
}
