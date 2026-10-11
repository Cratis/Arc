// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// An attributed value type that does not support enumerable covariance.
/// </summary>
/// <param name="Value">The event value.</param>
[ConsumedEvent]
public record struct ConsumedStruct(string Value);
