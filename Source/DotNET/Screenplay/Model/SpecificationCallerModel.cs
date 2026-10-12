// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents one exact principal witness, preserving roles and repeated claim values in source order.
/// </summary>
/// <param name="Authenticated">Whether the identity has a nonempty authentication type.</param>
/// <param name="Roles">The default role claims.</param>
/// <param name="Claims">The other claims, including repetitions.</param>
internal sealed record SpecificationCallerModel(bool Authenticated, IReadOnlyList<string> Roles, IReadOnlyList<KeyValuePair<string, string>> Claims);
