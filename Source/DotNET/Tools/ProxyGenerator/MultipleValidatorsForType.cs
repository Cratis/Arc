// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// The exception that is thrown when more than one validator is found for the same type.
/// </summary>
/// <param name="type">The type validated by both validators.</param>
/// <param name="first">The first validator.</param>
/// <param name="second">The second validator.</param>
internal class MultipleValidatorsForType(Type type, Type first, Type second)
    : Exception($"Multiple validators for '{type.FullName}': '{first.FullName}' and '{second.FullName}'.");
