// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a registered validator would run in a protected decision command.
/// </summary>
/// <param name="validatorType">The registered validator type that was refused.</param>
public class RegisteredValidatorRefusedInProtectedDecision(Type validatorType) : Exception($"Registered validator '{validatorType}' cannot run in a protected decision command; see Arc#2831.");
