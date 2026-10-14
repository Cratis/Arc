// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a String value read from the classified invocation refusal.
/// </summary>
/// <param name="Member">The portable refusal member (message or constraint).</param>
public record RefusalSource(string Member) : MappingSourceModel;
