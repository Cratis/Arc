// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// The exception that is thrown when a custom connection resolver is selected without a type implementing <see cref="IConnectionResolver"/>.
/// </summary>
/// <param name="type">The configured type, if any.</param>
public class CustomConnectionResolverNotConfigured(Type? type)
    : Exception(type is null
        ? $"A custom connection resolver is selected but no type is configured. Use '{nameof(ConnectionResolverOptions.UseCustom)}<TResolver>()'"
        : $"The custom connection resolver '{type.FullName}' does not implement '{nameof(IConnectionResolver)}'");
