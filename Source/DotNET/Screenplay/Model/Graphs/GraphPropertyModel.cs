// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model.Graphs;

/// <summary>A typed graph property.</summary>
/// <param name="Property">The property shape.</param>
/// <param name="IsKey">Whether it identifies a node.</param>
public record GraphPropertyModel(PropertyModel Property, bool IsKey);
