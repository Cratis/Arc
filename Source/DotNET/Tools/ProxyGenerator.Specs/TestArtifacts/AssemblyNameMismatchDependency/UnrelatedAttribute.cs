// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Specs.ActualDependency;

/// <summary>
/// Provides an unrelated attribute whose assembly can be omitted from a metadata resolver.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class UnrelatedAttribute : Attribute;
