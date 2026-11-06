// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Approvals;

/// <summary>
/// Marks a command as requiring an approval flow before its handler can execute.
/// </summary>
/// <remarks>
/// The requirement is inherited by derived command types. Without a registered approval flow,
/// Arc refuses execution and validation as unauthorized, returning HTTP 403 from mapped endpoints.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequiresApprovalAttribute : Attribute;
