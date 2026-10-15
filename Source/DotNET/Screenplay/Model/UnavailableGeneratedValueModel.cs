// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Retains a declared generated input independently of its strategy's recoverability.
/// </summary>
/// <param name="Name">The intended emitted name.</param>
/// <param name="Type">The declared value type.</param>
/// <param name="Location">The source location of the generation declaration.</param>
/// <param name="Failure">Why generation remains in code.</param>
public record UnavailableGeneratedValueModel(string Name, TypeReferenceModel Type, string Location, string Failure);
