// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Capabilities.for_CapabilitiesStartupValidation;

public class when_the_capability_group_has_a_port : given.a_capability_startup_validation
{
    Task Because() => Validate();

    [Fact] void should_allow_startup() => _result.IsCompletedSuccessfully.ShouldBeTrue();
}
