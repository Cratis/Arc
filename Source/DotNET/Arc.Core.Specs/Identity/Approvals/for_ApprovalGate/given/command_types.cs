// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Approvals.for_ApprovalGate.given;

internal static class command_types
{
    [RequiresApproval]
    internal record MarkedCommand;
    internal record DerivedCommand : MarkedCommand;
    internal record UnmarkedCommand;
}
