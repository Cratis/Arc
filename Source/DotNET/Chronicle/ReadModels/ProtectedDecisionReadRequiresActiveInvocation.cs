// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a protected decision read is attempted without an active command transaction or validation invocation.
/// </summary>
public class ProtectedDecisionReadRequiresActiveInvocation() : Exception("Protected decision reads require an active command transaction or validation invocation.");
