// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// Counterfeit attribute with the same full name as the fixture integration's attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ConsumedEventAttribute : Attribute;
