// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Marks a command as using advisory, unguarded reads. On plain read-model parameters this is only an
/// acknowledgement for tooling; parameter-level runtime opt-out is not supported.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Parameter | AttributeTargets.Method)]
public sealed class UnprotectedAttribute : Attribute, IUnprotectedDecisionAttribute;
