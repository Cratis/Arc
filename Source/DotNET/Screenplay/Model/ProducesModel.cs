// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents an event a command produces.
/// </summary>
/// <param name="EventName">The name of the event produced.</param>
/// <param name="When">The condition the production is guarded by, or <see langword="null"/> when it is unconditional.</param>
/// <param name="Mappings">The mappings from command input onto event properties.</param>
public record ProducesModel(
    string EventName,
    ConditionModel? When,
    IEnumerable<PropertyMappingModel> Mappings);
