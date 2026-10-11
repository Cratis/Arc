// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when a generated value parameter declaration is invalid.
/// </summary>
/// <param name="commandType">The command declaring the parameter.</param>
/// <param name="name">The parameter name.</param>
/// <param name="reason">The reason the declaration is invalid, without any generated value.</param>
public class InvalidGeneratedValueParameter(Type commandType, string name, string reason)
    : Exception($"Command '{commandType.FullName}' parameter '{name}' cannot use [GeneratedValue]: {reason}");
