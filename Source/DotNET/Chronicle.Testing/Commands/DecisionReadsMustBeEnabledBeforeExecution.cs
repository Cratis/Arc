// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when decision reads are enabled after the scenario has already executed or validated a command.
/// </summary>
public class DecisionReadsMustBeEnabledBeforeExecution() : Exception("Enable decision reads before the first Execute or Validate call.");
