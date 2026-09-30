// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when the event scenario is requested from a scenario in decision mode.
/// </summary>
public class EventScenarioUnavailableInDecisionMode() : Exception("EventScenario uses a separate log. In decision mode use EventLog and Given.ForEventSource(...).Events(...) instead.");
