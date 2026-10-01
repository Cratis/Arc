// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Opts a command into protected decision reads. The command pipeline fixes this profile before filters,
/// validators, or handler dependencies can run. A protected command only runs a discoverable validator whose only
/// public constructor is parameterless and that Arc constructed itself, with the rules and rule components it was
/// constructed with. A validator that takes constructor dependencies, or that a factory, an instance registration or
/// an explicit registration supplies instead of Arc's own instance, is refused. The certificate does not cover state
/// changed after construction, static members or service locators; keeping those out of the validators of a
/// protected decision is the developer's responsibility. Opaque or missing protected decision dependencies are refused.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ProtectedDecisionAttribute : Attribute, IProtectedDecisionAttribute;
