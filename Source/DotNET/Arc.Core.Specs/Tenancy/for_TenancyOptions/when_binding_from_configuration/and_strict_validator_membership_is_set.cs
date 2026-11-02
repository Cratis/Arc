// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Tenancy.for_TenancyOptions.when_binding_from_configuration;

public class and_strict_validator_membership_is_set : Specification
{
    ArcOptions _options;
    ArcOptions _defaults;

    void Establish()
    {
        _options = new ArcOptions();
        _defaults = new ArcOptions();
    }

    void Because() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["Tenancy:StrictValidatorMembership"] = "true" })
        .Build()
        .Bind(_options);

    [Fact] void should_bind_the_option() => _options.Tenancy.StrictValidatorMembership.ShouldBeTrue();
    [Fact] void should_be_off_by_default() => _defaults.Tenancy.StrictValidatorMembership.ShouldBeFalse();
}
