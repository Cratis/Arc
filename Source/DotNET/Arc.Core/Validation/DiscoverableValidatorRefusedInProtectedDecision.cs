// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation;

/// <summary>
/// The exception that is thrown when a discoverable validator would run in a protected decision command.
/// </summary>
/// <param name="validatorType">The discoverable validator type that was refused.</param>
public class DiscoverableValidatorRefusedInProtectedDecision(Type validatorType) : Exception($"Discoverable validator '{validatorType}' cannot run in a protected decision command; see Arc#2831.");
