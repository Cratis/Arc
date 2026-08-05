// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ValidatorInvoker;

/// <summary>
/// Two properties, so a validator can reject one and throw on the other.
/// </summary>
/// <param name="Name">The first property.</param>
/// <param name="Email">The second property.</param>
public record Subject(string Name, string Email);
