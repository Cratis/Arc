// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityStartupValidation;

public class when_a_block_builds_on_an_undefined_block : Specification
{
    AvailabilityStartupValidation _validation;
    Exception _error;

    void Establish()
    {
        var catalog = new AvailabilityCatalog().Add(new AvailabilityDefinition("Plus", AvailabilityExpression.With("Missing")));
        _validation = new AvailabilityStartupValidation(catalog, Substitute.For<IAvailabilityGate>());
    }

    async Task Because() => _error = await Catch.Exception(() => _validation.StartAsync(CancellationToken.None));

    [Fact] void should_refuse_to_start() => _error.ShouldBeOfExactType<UndefinedAvailabilityException>();
}
