// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when an operation declaration or its command boundary is unsupported.
/// </summary>
/// <param name="message">The violated contract.</param>
public class InvalidCommandOperation(string message) : Exception(message);
