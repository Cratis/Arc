// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents an invariant enforced when events are appended.
/// </summary>
/// <param name="Name">The name of the constraint.</param>
public abstract record ConstraintModel(string Name);
