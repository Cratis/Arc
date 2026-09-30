// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a <c>DecisionRead&lt;T&gt;</c> returned by <c>Provide</c> was not issued for the current command invocation.
/// </summary>
public class DecisionReadNotIssuedForInvocation() : Exception("A DecisionRead returned by Provide must be issued for this command invocation.");
