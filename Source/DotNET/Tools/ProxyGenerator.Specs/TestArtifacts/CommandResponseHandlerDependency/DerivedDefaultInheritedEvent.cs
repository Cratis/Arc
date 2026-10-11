// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// A derived value that inherits its base type's consumption attribute by default.
/// </summary>
/// <param name="Value">The event value.</param>
public record DerivedDefaultInheritedEvent(string Value) : DefaultInheritedEvent(Value);
