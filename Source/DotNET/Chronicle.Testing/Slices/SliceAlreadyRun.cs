// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Slices;

/// <summary>
/// The exception that is thrown when a slice is asked to run more than once.
/// </summary>
public class SliceAlreadyRun() : Exception("A slice can only be run once.");
