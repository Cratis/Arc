// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// A directly attributed value whose attribute does not permit inheritance.
/// </summary>
/// <param name="Value">The event value.</param>
[NonInheritedConsumedEvent]
public record NonInheritedEvent(string Value);
