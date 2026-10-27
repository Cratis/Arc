// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Slices;
using Cratis.Arc.Slices.for_SliceLocation.Billing.Invoices.Pay;
using Cratis.Execution;

namespace Cratis.Arc.Commands.for_CommandContext.when_getting_the_slice_location;

public class and_one_was_set : Specification
{
    readonly SliceLocation _explicit = new("Accounts", "Users", "Register");
    SliceLocation _result = null!;

    void Because() => _result = new CommandContext(CorrelationId.New(), typeof(PayInvoice), new PayInvoice(), [], new()) { SliceLocation = _explicit }.SliceLocation;

    [Fact] void should_use_the_one_that_was_set() => _result.ShouldEqual(_explicit);
}
