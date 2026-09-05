// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Arc.Chronicle.Commands.for_ConstraintViolationExtensions.when_converting;

/// <summary>
/// A constraint that names no property - one declared over the whole event rather than a single value - still names
/// itself. The identity of the constraint does not ride along on the details a violation happens to carry, so a
/// client can branch on it for every constraint, not only the ones that resolve to a field.
/// </summary>
public class without_the_property_name_in_details : Specification
{
    ConstraintViolation _violation;
    ValidationResult _result;

    void Establish() => _violation = new ConstraintViolation(
        EventTypeId.Unknown,
        EventSequenceNumber.Unavailable,
        ConstraintType.Unique,
        new ConstraintName("OneOnboardingPerAccount"),
        new ConstraintViolationMessage("The account has already been onboarded"),
        new ConstraintViolationDetails());

    void Because() => _result = _violation.ToValidationResult();

    [Fact] void should_name_the_constraint_that_rejected_it() => _result.ReasonDetail.ShouldEqual("OneOnboardingPerAccount");
    [Fact] void should_not_attribute_it_to_any_member() => _result.Members.ShouldBeEmpty();
}
