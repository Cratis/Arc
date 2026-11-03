// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// The exception thrown when an assertion on a returned command decision fails.
/// </summary>
/// <param name="message">The assertion failure message.</param>
public class CommandDecisionAssertionException(string message) : Exception(message);
