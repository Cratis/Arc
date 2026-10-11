// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// A value with only scalar handling declared.
/// </summary>
/// <param name="Value">The fixture value.</param>
[ScalarOnly]
public record ScalarOnlyValue(string Value);
