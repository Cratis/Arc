// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Arc.Capabilities.for_CapabilitiesStartupValidation;

public class when_the_capability_group_has_no_port : Specification
{
    Exception? _exception;
    async Task Because()
    {
        var validation = new CapabilitiesStartupValidation(Options.Create(new ArcOptions()), Options.Create(new CapabilitiesOptions()));
        try { await validation.StartingAsync(CancellationToken.None); }
        catch (Exception exception) { _exception = exception; }
    }
    [Fact] void should_refuse_startup() => _exception.ShouldBeOfExactType<CapabilityEndpointGroupHasNoPort>();
    [Fact] void should_explain_how_to_assign_a_port() => _exception!.Message.ShouldContain("EndpointGroups.Assign");
}
