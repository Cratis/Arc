// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents one artifact a specification names, together with the values it states for it.
/// </summary>
/// <param name="Name">The name of the event, read model or command being named.</param>
/// <param name="Kind">What the named artifact is.</param>
/// <param name="Values">The values the specification states, in the order the source declares them.</param>
/// <remarks>
/// A <c>given</c>, a <c>when</c> and a <c>then</c> are the same shape - a name and a list of values - so one model
/// covers all three and the step it belongs to says which of them it is.
/// </remarks>
public record SpecificationStateModel(string Name, SpecificationStateKind Kind, IEnumerable<PropertyMappingModel> Values);
