// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Slices;

/// <summary>
/// The exception that is thrown when a slice's outcome is read before its run has completed.
/// </summary>
public class SliceNotRun() : Exception("Run the slice to completion before reading its outcome.");
