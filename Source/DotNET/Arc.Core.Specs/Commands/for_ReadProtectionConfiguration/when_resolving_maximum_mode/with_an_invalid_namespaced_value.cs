// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Commands.for_ReadProtectionConfiguration.when_resolving_maximum_mode;

public class with_an_invalid_namespaced_value : Specification
{
    ReadProtectionMode _maximum;
    IConfiguration _configuration;

    void Establish() => _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Cratis:Arc:ReadProtection:MaximumMode"] = "invalid",
        ["ReadProtection:MaximumMode"] = "Off"
    }).Build();

    void Because() => _maximum = ReadProtectionConfiguration.MaximumMode(_configuration);

    [Fact] void should_leave_declarations_unchanged() => _maximum.ShouldEqual(ReadProtectionMode.On);
}
