// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities.for_CapabilitiesStartupValidation;

public class when_the_capability_group_has_a_port : Specification
{
    Task _result;
    void Because()
    {
        var arc = new ArcOptions();
        arc.EndpointGroups.Assign("capabilities", 42001);
        _result = new CapabilitiesStartupValidation(Options.Create(arc), Options.Create(new CapabilitiesOptions())).StartingAsync(CancellationToken.None);
    }
    [Fact] void should_allow_startup() => _result.IsCompletedSuccessfully.ShouldBeTrue();
}
