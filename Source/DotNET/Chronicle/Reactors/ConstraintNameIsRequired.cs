// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// The exception that is thrown when a named constraint selector has a null, empty or whitespace name.
/// </summary>
public class ConstraintNameIsRequired() : Exception("A named constraint selector requires a nonempty, non-whitespace name.");
