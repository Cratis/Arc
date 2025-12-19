// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the result of a query renderer.
/// </summary>
/// <param name="TotalItems">Total items.</param>
/// <param name="Data">The rendered data.</param>
public record QueryRendererResult(int TotalItems, object Data);