// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Validation;

/// <summary>
/// The exception thrown when an assertion on a validator fails.
/// </summary>
/// <param name="message">The assertion failure message.</param>
public class ValidatorAssertionException(string message) : Exception(message);
