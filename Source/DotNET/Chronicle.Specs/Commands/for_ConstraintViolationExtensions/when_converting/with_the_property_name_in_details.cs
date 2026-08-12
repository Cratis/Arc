// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Arc.Chronicle.Commands.for_ConstraintViolationExtensions.when_converting;

/// <summary>
/// The full conversion: the violation arrives knowing which constraint rejected it, and every part of that has to
/// survive the crossing into Arc's validation vocabulary - the message, the member it belongs to, the category of
/// rejection, and the identity of the constraint itself.
/// </summary>
public class with_the_property_name_in_details : Specification
{
    ConstraintViolation _violation;
    ValidationResult _result;

    void Establish() => _violation = new ConstraintViolation(
        EventTypeId.Unknown,
        EventSequenceNumber.Unavailable,
        ConstraintType.Unique,
        new ConstraintName("UniqueOrganizationNumber"),
        new ConstraintViolationMessage("The organization number is already in use"),
        new ConstraintViolationDetails { [WellKnownConstraintDetailKeys.PropertyName] = "OrganizationNumber" });

    void Because() => _result = _violation.ToValidationResult();

    [Fact] void should_name_the_constraint_that_rejected_it() => _result.ReasonDetail.ShouldEqual("UniqueOrganizationNumber");
    [Fact] void should_say_the_rejection_is_a_constraint_violation() => _result.Reason.ShouldEqual(ValidationResultReason.ConstraintViolation);
    [Fact] void should_carry_the_violation_message() => _result.Message.ShouldEqual("The organization number is already in use");
    [Fact] void should_attribute_it_to_the_camel_cased_member() => _result.Members.ShouldContainOnly("organizationNumber");
    [Fact] void should_be_an_error() => _result.Severity.ShouldEqual(ValidationResultSeverity.Error);
}
