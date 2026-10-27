// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Slices;
using Cratis.Arc.Slices.for_SliceLocation.Billing.Invoices.Pay;
using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandContext.when_getting_the_slice_location;

public class and_none_was_set : Specification
{
    SliceLocation _result = null!;

    void Because() => _result = new CommandContext(CorrelationId.New(), typeof(PayInvoice), new PayInvoice(), [], new()).SliceLocation;

    [Fact] void should_resolve_it_from_the_command_type() => _result.ShouldEqual(new SliceLocation("Billing", "Invoices", "Pay"));
}
