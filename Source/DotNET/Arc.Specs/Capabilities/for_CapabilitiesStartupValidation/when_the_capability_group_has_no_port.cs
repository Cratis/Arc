// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilitiesStartupValidation;

public class when_the_capability_group_has_no_port : given.a_capability_startup_validation
{
    void Establish()
    {
        _arc = new ArcOptions();
        _installation = new(null, null);
        _services.Clear();
    }

    Task Because() => Validate();

    [Fact] void should_refuse_startup() => _error.ShouldBeOfExactType<CapabilityEndpointGroupHasNoPort>();
    [Fact] void should_explain_how_to_assign_a_port() => _error!.Message.ShouldContain("EndpointGroups.Assign");
}
