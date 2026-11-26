// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Slices.for_SliceLocation.Billing.Invoices.Pay;

namespace Cratis.Arc.Introspection.for_ApplicationStructureBuilder;

public class when_building : Specification
{
    ApplicationStructure _structure;

    void Because() => _structure = ApplicationStructureBuilder.Build(
        [typeof(PayInvoice)],
        [(typeof(PayInvoice), "Payments")]);

    [Fact] void should_have_the_module() => _structure.Modules.Select(_ => _.Path).ShouldContainOnly("Billing");
    [Fact] void should_nest_the_feature_in_the_module() => _structure.Modules[0].Features.Select(_ => _.Path).ShouldContainOnly("Billing/Invoices");
    [Fact] void should_nest_the_slice_in_the_feature() => _structure.Modules[0].Features[0].Slices.Select(_ => _.Path).ShouldContainOnly("Billing/Invoices/Pay");
    [Fact] void should_hold_the_command_and_the_read_model_of_the_slice() => _structure.Modules[0].Features[0].Slices[0].Parts.Select(_ => _.Kind).ShouldContainOnly(ApplicationStructureBuilder.CommandKind, ApplicationStructureBuilder.ReadModelKind);
}
