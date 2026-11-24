// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Identity.Approvals;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization.given;

[Command]
[RequiresApproval]
public record ApprovalCommand
{
    public void Handle(PolicyBehavior behavior) => behavior.HandlerCalls++;
}
