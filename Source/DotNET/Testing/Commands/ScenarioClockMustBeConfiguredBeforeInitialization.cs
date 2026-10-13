// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// The exception that is thrown when the scenario clock changes after the service provider is initialized.
/// </summary>
public class ScenarioClockMustBeConfiguredBeforeInitialization() : Exception(
    "Clock() must be configured before the first Execute() or Validate() call initializes the scenario. Create a new scenario to change its clock.");
