// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// Identifies handled values without passing the attribute to derived types.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class NonInheritedConsumedEventAttribute : Attribute;
