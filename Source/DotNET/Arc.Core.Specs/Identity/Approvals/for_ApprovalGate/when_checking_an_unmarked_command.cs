// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Approvals.for_ApprovalGate;

public class when_checking_an_unmarked_command : Specification
{
    bool _result;
    void Because() => _result = ApprovalGate.RequiresApproval(typeof(given.command_types.UnmarkedCommand));
    [Fact] void should_not_require_approval() => _result.ShouldBeFalse();
}
