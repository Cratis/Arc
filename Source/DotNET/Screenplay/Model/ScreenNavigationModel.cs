// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a literal route resolved to an emitted screen and its literal parameters.
/// </summary>
/// <param name="Screen">The bare screen name or owner-qualified reference.</param>
/// <param name="Route">The literal destination passed to the router.</param>
/// <param name="Parameters">The values supplied by parameterized path segments.</param>
public record ScreenNavigationModel(string Screen, string Route, IReadOnlyDictionary<string, string> Parameters);
