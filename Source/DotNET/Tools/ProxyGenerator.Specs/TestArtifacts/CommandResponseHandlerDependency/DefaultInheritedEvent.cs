// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// A value carrying an attribute with default inheritance behavior.
/// </summary>
/// <param name="Value">The event value.</param>
[DefaultInheritedConsumedEvent]
public record DefaultInheritedEvent(string Value);
