// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Approvals;

namespace Cratis.Arc.Mcp.for_McpToolCeiling.given;

[RequiresApproval]
public record ApprovalRequiredCommand(string Value);
