// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Opts a command into protected decision reads. The command pipeline fixes this profile before filters,
/// validators, or handler dependencies can run. A protected command only runs a discoverable validator that Arc can
/// certify: its only public constructor is parameterless and Arc constructed the instance itself. A validator that
/// takes constructor dependencies, or that a factory, instance, explicit registration or decorator supplies, is
/// refused. Opaque or missing protected decision dependencies are refused.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ProtectedDecisionAttribute : Attribute, IProtectedDecisionAttribute;
