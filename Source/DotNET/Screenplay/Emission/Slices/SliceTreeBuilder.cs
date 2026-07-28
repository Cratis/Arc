// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Slices;

/// <summary>
/// Arranges slices into the module and feature tree of a Screenplay document.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
public class SliceTreeBuilder(IScreenplayNaming naming)
{
    /// <summary>
    /// Builds the module holding every slice, nesting features after the namespace segments of each slice.
    /// </summary>
    /// <param name="slices">The slices to arrange, keyed on the namespace they live in.</param>
    /// <param name="moduleName">The name of the module.</param>
    /// <param name="segmentsToSkip">The number of leading namespace segments to skip.</param>
    /// <returns>The modules of the document.</returns>
    public IEnumerable<ModuleSyntax> Build(IEnumerable<PlacedSlice> slices, string moduleName, int segmentsToSkip)
    {
        var root = new FeatureNode();

        foreach (var placed in slices)
        {
            var path = GetFeaturePath(placed.Namespace, moduleName, segmentsToSkip);
            root.Resolve(path).Slices.Add(placed.Slice);
        }

        var features = root.BuildChildren();

        return features.Length == 0 ? [] : [new ModuleSyntax(moduleName, [], features, SourceLocation.Start)];
    }

    /// <summary>
    /// Resolves the feature path a slice is placed under, excluding the slice itself.
    /// </summary>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <param name="moduleName">The name of the module.</param>
    /// <param name="segmentsToSkip">The number of leading namespace segments to skip.</param>
    /// <returns>The feature names, outermost first.</returns>
    string[] GetFeaturePath(string @namespace, string moduleName, int segmentsToSkip)
    {
        var segments = @namespace
            .Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Skip(segmentsToSkip)
            .Select(naming.ToDeclarationName)
            .ToArray();

        if (segments.Length > 1 && string.Equals(segments[0], moduleName, StringComparison.Ordinal))
        {
            segments = [.. segments.Skip(1)];
        }

        return segments.Length switch
        {
            0 => [moduleName],
            1 => [segments[0]],
            _ => [.. segments[..^1]]
        };
    }

    /// <summary>
    /// Represents a node while the feature tree is being assembled.
    /// </summary>
    sealed class FeatureNode
    {
        public SortedDictionary<string, FeatureNode> Children { get; } = new(StringComparer.Ordinal);

        public List<SliceSyntax> Slices { get; } = [];

        public FeatureNode Resolve(IEnumerable<string> path)
        {
            var current = this;
            foreach (var segment in path)
            {
                if (!current.Children.TryGetValue(segment, out var child))
                {
                    child = new FeatureNode();
                    current.Children[segment] = child;
                }

                current = child;
            }

            return current;
        }

        public FeatureSyntax[] BuildChildren() =>
            [.. Children.Select(_ => new FeatureSyntax(
                _.Key,
                _.Value.BuildChildren(),
                [.. _.Value.Slices.OrderBy(slice => slice.Name, StringComparer.Ordinal)],
                SourceLocation.Start))];
    }
}
