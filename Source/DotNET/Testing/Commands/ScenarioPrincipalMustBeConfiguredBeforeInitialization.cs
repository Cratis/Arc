// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// The exception that is thrown when the scenario principal changes after the service provider is initialized.
/// </summary>
public class ScenarioPrincipalMustBeConfiguredBeforeInitialization() : Exception(
    "The scenario principal must be configured before the first Execute or Validate call. Create a new scenario to change its principal.");
