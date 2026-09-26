// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Introspection;

/// <summary>
/// The exception that is thrown when the host cannot enforce the requested introspection access settings.
/// </summary>
/// <param name="message">The reason the configuration cannot be enforced.</param>
public class InvalidIntrospectionConfiguration(string message) : Exception(message);
