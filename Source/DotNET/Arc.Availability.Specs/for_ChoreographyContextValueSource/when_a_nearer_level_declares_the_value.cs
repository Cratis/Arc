// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_ChoreographyContextValueSource;

public class when_a_nearer_level_declares_the_value : given.a_value_host
{
    object _inTheModule;
    object _atTheApplication;

    async Task Because()
    {
        _inTheModule = await ValuesFor(typeof(given.Fleet.Billing.InvoiceCommand)).GetAsync("vessel");
        _atTheApplication = await ValuesFor(typeof(given.an_availability_host.PlusCommand)).GetAsync("vessel");
    }

    [Fact] void should_use_the_nearest_declaration_for_a_part_in_the_module() => _inTheModule.ShouldEqual("fleet");
    [Fact] void should_use_the_application_declaration_for_other_parts() => _atTheApplication.ShouldEqual("app");
}
