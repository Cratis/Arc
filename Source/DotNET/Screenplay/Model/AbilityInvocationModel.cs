// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>Represents one <c>invokes &lt;System&gt;.&lt;Ability&gt;</c> read from an operation phase.</summary>
/// <param name="System">The system, named as in the model.</param>
/// <param name="Ability">The ability name.</param>
/// <param name="Inputs">The ability inputs and the operation inputs they take their value from.</param>
/// <param name="Parameters">The ability's declared inputs: the collaborator method's parameters, without the cancellation token.</param>
public record AbilityInvocationModel(string System, string Ability, IReadOnlyList<PropertyMappingModel> Inputs, IReadOnlyList<PropertyModel> Parameters);
