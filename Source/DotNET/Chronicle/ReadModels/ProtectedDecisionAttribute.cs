// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Opts a command into protected decision reads. The command pipeline fixes this profile before filters,
/// validators, or handler dependencies can run. All discoverable validators are refused for protected commands
/// before construction, including parameterless, factory-provided, registered, and unregistered validators.
/// Opaque or missing protected decision dependencies are refused.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ProtectedDecisionAttribute : Attribute, IProtectedDecisionAttribute;
