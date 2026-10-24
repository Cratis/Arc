// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_using_certified_validator_construction.given;

public class an_ordinary_ownership_factory : a_certified_command
{
    protected int _calls;

    void Establish()
    {
        _scenario.Services.AddTransient<CertifiedValidator>(_ =>
        {
            _calls++;
            throw new InvalidOperationException("ownership factory must not run");
        });
        Build();
    }
}
