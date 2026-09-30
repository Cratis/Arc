// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandDecisionPolicy;

#pragma warning disable SA1649, SA1402

[AttributeUsage(AttributeTargets.Class)]
public sealed class ProtectedForSpecsAttribute : Attribute, IProtectedDecisionAttribute;

[AttributeUsage(AttributeTargets.Class)]
public sealed class UnprotectedForSpecsAttribute : Attribute, IUnprotectedDecisionAttribute;

[ProtectedForSpecs]
public record ProtectedCommand;

[ProtectedForSpecs]
[UnprotectedForSpecs]
public record ConflictingCommand;

public record UnmarkedCommand : IProtectedDecisionAttribute;

#pragma warning restore SA1649, SA1402
