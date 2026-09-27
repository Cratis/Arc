// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Opts a command into protected decision reads. The command pipeline fixes this profile before filters,
/// validators, or handler dependencies can run. Registered validator instances and factories are ignored: validators
/// are freshly constructed per invocation with only direct DecisionRead&lt;T&gt; or IDecisionReads dependencies.
/// Opaque or missing decision dependencies are refused.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ProtectedDecisionAttribute : Attribute, IProtectedDecisionCommand;
