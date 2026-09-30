// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when competing events are queued on a scenario that did not enable decision reads.
/// </summary>
public class ConcurrentAppendRequiresDecisionReads() : Exception("AppendConcurrently requires UseDecisionReads().");
