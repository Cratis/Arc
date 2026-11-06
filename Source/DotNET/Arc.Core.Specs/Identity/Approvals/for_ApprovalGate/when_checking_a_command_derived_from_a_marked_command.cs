// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Approvals.for_ApprovalGate;

public class when_checking_a_command_derived_from_a_marked_command : Specification
{
    bool _result;
    void Because() => _result = ApprovalGate.RequiresApproval(typeof(given.command_types.DerivedCommand));
    [Fact] void should_inherit_the_approval_requirement() => _result.ShouldBeTrue();
}
