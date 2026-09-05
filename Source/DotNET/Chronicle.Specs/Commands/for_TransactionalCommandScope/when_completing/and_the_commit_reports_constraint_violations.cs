// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing;

public class and_the_commit_reports_constraint_violations : given.a_transactional_command_scope
{
    CommandResult _result;

    void Establish()
    {
        var violation = new ConstraintViolation(
            EventTypeId.Unknown,
            EventSequenceNumber.Unavailable,
            ConstraintType.Unknown,
            new ConstraintName("UniqueOrganizationNumber"),
            new ConstraintViolationMessage("Organization number must be unique"),
            new ConstraintViolationDetails());
        _unitOfWork.GetConstraintViolations().Returns([violation]);
        _result = CommandResult.Success(_correlationId);
    }

    async Task Because()
    {
        _scope.Begin(_context);
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_surface_the_violation_as_a_validation_error() => _result.ValidationResults.Any(validationResult => validationResult.Message.Contains("Organization number must be unique")).ShouldBeTrue();
}
