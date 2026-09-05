// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Specs.SourceFileResolverFixture;

/// <summary>
/// A drawing whose generated proxy requires enumerable interface metadata.
/// </summary>
public class GeneratedMetadataDrawing
{
    /// <summary>
    /// Gets or sets the shapes in the drawing.
    /// </summary>
    public IEnumerable<IGeneratedMetadataShape> Shapes { get; set; } = [];
}
