// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Exception thrown when a command returns a native union without an active non-null case.
/// </summary>
public class InvalidCommandUnionValue() : Exception("A command union must contain an active non-null value.");
