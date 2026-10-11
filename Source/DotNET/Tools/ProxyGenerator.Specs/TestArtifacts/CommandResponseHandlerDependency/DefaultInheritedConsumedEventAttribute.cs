// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// Identifies handled values using the default attribute inheritance behavior.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DefaultInheritedConsumedEventAttribute : Attribute;
