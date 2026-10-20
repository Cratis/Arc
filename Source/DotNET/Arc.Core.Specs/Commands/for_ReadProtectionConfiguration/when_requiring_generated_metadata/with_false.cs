// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Commands.for_ReadProtectionConfiguration.when_requiring_generated_metadata;

public class with_false : Specification
{
    bool _required;
    IConfiguration _configuration;

    void Establish() => _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
            ["ReadProtection:RequireGeneratedMetadata"] = "false"
    }).Build();

    void Because() => _required = ReadProtectionConfiguration.RequireGeneratedMetadata(_configuration);

    [Fact] void should_resolve_the_metadata_requirement() => _required.ShouldEqual(!RuntimeFeature.IsDynamicCodeSupported);
}
