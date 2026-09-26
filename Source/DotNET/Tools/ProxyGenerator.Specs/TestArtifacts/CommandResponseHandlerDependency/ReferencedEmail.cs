// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// A concept declared in a referenced assembly.
/// </summary>
/// <param name="Value">The email value.</param>
public record ReferencedEmail(string Value) : ConceptAs<string>(Value);
