// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Routes;

/// <summary>
/// Identifies a retained screen by its original source path, before relative file emission.
/// </summary>
/// <param name="Namespace">The declaring slice.</param>
/// <param name="Name">The screen name.</param>
/// <param name="Path">The original file path.</param>
internal sealed record RouteScreen(string Namespace, string Name, string Path);
