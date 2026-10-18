// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Describes one operation performed by Arc, retained by descriptor reference.
/// </summary>
/// <param name="IsConvention">Whether Arc inserted a convention binding.</param>
/// <param name="Configure">The exact Arc-owned validator configuration callback.</param>
/// <param name="VerifyRead">The direct Chronicle verifier for a recognized read leaf.</param>
/// <param name="IsCommandReader">Whether this is a command-aware reader binding.</param>
/// <param name="IsSupersededReader">Whether this is a framework raw reader superseded by a command-aware reader.</param>
/// <param name="IsChronicleClientReader">Whether Arc observed the Chronicle registration call inserting this reader.</param>
internal sealed record ProtectedDecisionRegistrationOrigin(
    bool IsConvention = false,
    Action<object>? Configure = null,
    Action<object?>? VerifyRead = null,
    bool IsCommandReader = false,
    bool IsSupersededReader = false,
    bool IsChronicleClientReader = false);
