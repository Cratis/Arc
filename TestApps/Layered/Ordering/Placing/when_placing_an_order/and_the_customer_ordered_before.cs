// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Xunit;

namespace Layered.Ordering.Placing.when_placing_an_order;

/// <summary>
/// A scenario stating the customer it is about as a well known one the contracts project declares.
/// </summary>
/// <remarks>
/// Naming <see cref="KnownCustomers.House"/> is what makes this fixture worth having. Reading the scenario means
/// following that member back to where it was made, and where it was made is another project - so the declaration
/// belongs to a compilation other than the one reading the scenario. Following it through the wrong compilation is
/// not a wrong answer but a crash.
/// <para>
/// Not crashing is the smaller half. The identity is made on the spot, so a document leaving it out says exactly what
/// the source says and nothing is reported. A reader that merely declined to look across the project boundary would
/// also leave it out - and would report the value as one nothing was recovered from, which is what the expectations
/// beside this fixture hold it to.
/// </para>
/// </remarks>
public class and_the_customer_ordered_before
{
    readonly CommandScenario<PlaceOrder> _scenario = new();
    CommandResult _result = null!;

    void Establish() =>
        _scenario.Given.ForEventSource(KnownCustomers.House).Events(new OrderPlaced("COFFEE-250G"));

    async Task Because() => _result = await _scenario.Execute(new PlaceOrder(KnownCustomers.House, "COFFEE-1KG"));

    [Fact] void should_be_successful() => _result.ShouldBeSuccessful();
    [Fact] async Task should_have_placed_the_order() => await _scenario.ShouldHaveAppendedEvent<PlaceOrder, OrderPlaced>(KnownCustomers.House);
}
