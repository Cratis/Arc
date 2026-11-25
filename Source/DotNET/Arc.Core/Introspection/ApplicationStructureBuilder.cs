// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Slices;

namespace Cratis.Arc.Introspection;

/// <summary>
/// Builds the <see cref="ApplicationStructure"/> from the commands and read models of an application.
/// </summary>
public static class ApplicationStructureBuilder
{
    /// <summary>
    /// The separator of the segments of a path.
    /// </summary>
    public const char PathSeparator = '/';

    /// <summary>
    /// The kind of a part that is a command.
    /// </summary>
    public const string CommandKind = "Command";

    /// <summary>
    /// The kind of a part that is a read model.
    /// </summary>
    public const string ReadModelKind = "ReadModel";

    /// <summary>
    /// Gets the path segments of the slice a type belongs to: the module, each feature it is nested in, then the slice.
    /// </summary>
    /// <param name="type">The command type, or the type declaring the queries.</param>
    /// <returns>The segments, without the empty ones.</returns>
    public static IReadOnlyList<string> SegmentsOf(Type type)
    {
        var location = SliceLocation.For(type);
        return [.. new[] { location.Module.Value, location.Feature.Value, location.Slice.Value }
            .SelectMany(_ => _.Split('.', StringSplitOptions.RemoveEmptyEntries))];
    }

    /// <summary>
    /// Determines whether a path is, or sits beneath, another path.
    /// </summary>
    /// <param name="path">The segments of the path to test.</param>
    /// <param name="ancestor">The candidate ancestor, with segments separated by '/'.</param>
    /// <returns>True when the path equals the ancestor or is nested in it.</returns>
    public static bool IsWithin(IReadOnlyList<string> path, string ancestor)
    {
        var segments = ancestor.Split(PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 && segments.Length <= path.Count && segments.SequenceEqual(path.Take(segments.Length), StringComparer.Ordinal);
    }

    /// <summary>
    /// Builds the structure.
    /// </summary>
    /// <param name="commands">The command types.</param>
    /// <param name="readModels">The types that declare queries, paired with the name of the read model.</param>
    /// <returns>The <see cref="ApplicationStructure"/>.</returns>
    public static ApplicationStructure Build(IEnumerable<Type> commands, IEnumerable<(Type Declaring, string Name)> readModels)
    {
        var parts = commands
            .Select(type => (Type: type, Part: new StructurePart(type.Name, type.FullName ?? type.Name, CommandKind)))
            .Concat(readModels.Select(_ => (Type: _.Declaring, Part: new StructurePart(_.Name, _.Declaring.FullName ?? _.Declaring.Name, ReadModelKind))))
            .GroupBy(_ => $"{_.Part.Kind}|{_.Part.FullName}")
            .Select(_ => _.First());

        var modules = new SortedDictionary<string, Node>(StringComparer.Ordinal);
        foreach (var (type, part) in parts)
        {
            var location = SliceLocation.For(type);
            if (!modules.TryGetValue(location.Module.Value, out var node))
            {
                node = new Node(location.Module.Value, location.Module.Value);
                modules[location.Module.Value] = node;
            }

            foreach (var feature in location.Feature.Value.Split('.', StringSplitOptions.RemoveEmptyEntries))
            {
                node = node.Child(feature);
            }

            node.Slice(location.Slice.Value.Length == 0 ? part.Name : location.Slice.Value).Parts.Add(part);
        }

        return new([.. modules.Values.Select(_ => new StructureModule(_.Name, _.Path, _.Features(), _.Slices()))]);
    }

    static string Join(string parent, string name) => parent.Length == 0 ? name : $"{parent}{PathSeparator}{name}";

    sealed class Node(string name, string path)
    {
        readonly SortedDictionary<string, Node> _children = new(StringComparer.Ordinal);
        readonly SortedDictionary<string, SliceNode> _slices = new(StringComparer.Ordinal);

        public string Name { get; } = name;

        public string Path { get; } = path;

        public Node Child(string name)
        {
            if (!_children.TryGetValue(name, out var child))
            {
                child = new Node(name, Join(Path, name));
                _children[name] = child;
            }

            return child;
        }

        public SliceNode Slice(string name)
        {
            if (!_slices.TryGetValue(name, out var slice))
            {
                slice = new SliceNode(name, Join(Path, name));
                _slices[name] = slice;
            }

            return slice;
        }

        public StructureFeature[] Features() =>
            [.. _children.Values.Select(_ => new StructureFeature(_.Name, _.Path, _.Features(), _.Slices()))];

        public StructureSlice[] Slices() =>
            [.. _slices.Values.Select(_ => new StructureSlice(_.Name, _.Path, [.. _.Parts.OrderBy(part => part.Name, StringComparer.Ordinal)]))];
    }

    sealed class SliceNode(string name, string path)
    {
        public string Name { get; } = name;

        public string Path { get; } = path;

        public List<StructurePart> Parts { get; } = [];
    }
}
