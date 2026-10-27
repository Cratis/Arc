// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Slices.for_SliceLocation.Billing.Invoices.Pay;

namespace Cratis.Arc.Slices.for_SliceLocation;

public class when_resolving_for_a_type : Specification
{
    SliceLocation _result = null!;

    void Because() => _result = SliceLocation.For(typeof(PayInvoice));

    [Fact] void should_use_the_namespace_root_declared_on_the_assembly() => _result.ShouldEqual(new SliceLocation("Billing", "Invoices", "Pay"));
    [Fact] void should_render_the_parts_as_a_path() => _result.ToString().ShouldEqual("Billing/Invoices/Pay");
}
