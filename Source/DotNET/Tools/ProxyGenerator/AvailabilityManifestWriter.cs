// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Availability.ManifestFile;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Builds the availability manifest that the build can see: every part with where it lives, the gates declared with
/// attributes (and on scope markers), and what an operator can suspend.
/// </summary>
/// <remarks>
/// The generator reads the assemblies without running them, so a gate registered in code at startup, the body of a block
/// and a typed condition that is code are not in this file. A condition that is code is listed as a named condition.
/// <c>ManifestFileBuilder</c> in <c>Cratis.Arc.Availability</c> writes the complete manifest from the running application.
/// </remarks>
static class AvailabilityManifestWriter
{
    const string ForAttribute = "AvailableForAttribute";
    const string WithAttribute = "AvailableWithAttribute";
    const string BlockAttribute = "AvailableAttribute";
    const string WhenAttribute = "AvailableWhenAttribute";

    /// <summary>
    /// Builds the manifest.
    /// </summary>
    /// <param name="commands">The command descriptors the generator found.</param>
    /// <param name="queries">The query descriptors the generator found.</param>
    /// <param name="origin">What wrote the manifest.</param>
    /// <returns>The <see cref="ManifestFileDocument"/>.</returns>
    internal static ManifestFileDocument Build(IEnumerable<CommandDescriptor> commands, IEnumerable<QueryDescriptor> queries, string origin)
    {
        var found = new Dictionary<string, (Type Type, string Kind, SliceLocationDescriptor Location)>();
        foreach (var command in commands.Where(_ => HasAttribute(_.Type, "CommandAttribute")))
        {
            found.TryAdd(command.PartName, (command.Type, "command", command.SliceLocation));
        }

        foreach (var query in queries.Where(_ => HasAttribute(_.Type, "ReadModelAttribute")))
        {
            found.TryAdd(query.PartName, (query.Type, "readModel", query.SliceLocation));
        }

        var scopes = ScopeMarkers(found.Values.Select(_ => _.Type.Assembly).Distinct());
        var parts = found
            .OrderBy(_ => _.Key, StringComparer.Ordinal)
            .Select(_ => new ManifestPart(
                _.Key,
                _.Value.Kind,
                _.Value.Location.Module,
                _.Value.Location.Feature,
                _.Value.Location.Slice,
                [.. GatesOf(_.Value.Type, scopes)]))
            .ToList();

        var conditions = parts
            .SelectMany(_ => _.Gates)
            .Concat(scopes.Select(_ => _.Gate))
            .SelectMany(NamedConditions)
            .DistinctBy(_ => _.Name)
            .Select(_ => new ManifestNamedConditionEntry(_.Name, _.Description))
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
            .ToList();

        return new(
            ManifestFileDocument.FormatName,
            ManifestFileDocument.CurrentVersion,
            origin,
            [],
            [],
            conditions,
            [],
            [.. scopes.OrderBy(_ => _.Namespace, StringComparer.Ordinal)],
            parts,
            [.. Suspendable(found.Values)]);
    }

    static IEnumerable<ManifestSuspendable> Suspendable(IEnumerable<(Type Type, string Kind, SliceLocationDescriptor Location)> parts)
    {
        var entries = new Dictionary<string, ManifestSuspendable>();
        foreach (var (type, _, location) in parts)
        {
            var segments = new List<string>();
            foreach (var (name, kind) in new[] { (location.Module, "module"), (location.Feature, "feature"), (location.Slice, "slice") })
            {
                if (name.Length == 0)
                {
                    continue;
                }

                segments.Add(name);
                var path = string.Join('/', segments);
                entries.TryAdd(path, new ManifestSuspendable(kind, path, name));
            }

            var partPath = segments.Count == 0 ? (type.FullName ?? type.Name) : $"{string.Join('/', segments)}/{type.FullName ?? type.Name}";
            entries[partPath] = new ManifestSuspendable("part", partPath, type.Name);
        }

        return entries.Values.OrderBy(_ => _.Path, StringComparer.Ordinal);
    }

    static List<ManifestScope> ScopeMarkers(IEnumerable<Assembly> assemblies)
    {
        var scopes = new List<ManifestScope>();
        foreach (var assembly in assemblies)
        {
            foreach (var type in TypesOf(assembly).Where(IsScopeMarker))
            {
                scopes.AddRange(Gates(type).Select(gate => new ManifestScope(type.Namespace!, gate)));
            }
        }

        return scopes;
    }

    static IEnumerable<Type> TypesOf(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

    static bool IsScopeMarker(Type type)
    {
        if (!type.IsClass || type.IsAbstract || string.IsNullOrEmpty(type.Namespace))
        {
            return false;
        }

        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.Name == "AvailabilityScope")
            {
                return true;
            }
        }

        return false;
    }

    static List<ManifestExpression> GatesOf(Type part, List<ManifestScope> scopes)
    {
        var gates = scopes
            .Where(_ => InNamespace(part, _.Namespace))
            .OrderBy(_ => _.Namespace.Length)
            .Select(_ => _.Gate)
            .ToList();
        gates.AddRange(Gates(part));
        return gates;
    }

    static bool InNamespace(Type type, string @namespace) =>
        type.Namespace is { } ns && (ns == @namespace || ns.StartsWith(@namespace + ".", StringComparison.Ordinal));

    static IEnumerable<ManifestExpression> Gates(Type type)
    {
        var typed = new List<ManifestExpression>();
        var legacy = new List<ManifestExpression>();
        foreach (var attribute in type.GetCustomAttributesData())
        {
            var attributeType = attribute.AttributeType;
            var name = attributeType.IsGenericType ? attributeType.Name[..attributeType.Name.IndexOf('`')] : attributeType.Name;
            var arguments = attributeType.IsGenericType ? attributeType.GetGenericArguments() : [];
            switch (name)
            {
                case ForAttribute when arguments.Length > 0:
                    typed.Add(Any(arguments.Select(_ => (ManifestExpression)new ManifestFor(_.Name))));
                    break;
                case ForAttribute:
                    legacy.Add(Any(StringsOf(attribute).Select(_ => (ManifestExpression)new ManifestFor(_))));
                    break;
                case BlockAttribute when arguments.Length == 1:
                    typed.Add(new ManifestWith(arguments[0].Name));
                    break;
                case WhenAttribute when arguments.Length == 1:
                    typed.Add(new ManifestWhen(new ManifestNamedCondition(arguments[0].FullName ?? arguments[0].Name, string.Empty)));
                    break;
                case WithAttribute when attribute.ConstructorArguments.Count == 1 && attribute.ConstructorArguments[0].Value is string block:
                    legacy.Add(new ManifestWith(block));
                    break;
            }
        }

        return typed.Concat(legacy);
    }

    static ManifestExpression Any(IEnumerable<ManifestExpression> items)
    {
        var list = items.ToList();
        return list.Count == 1 ? list[0] : new ManifestAny(list);
    }

    static IEnumerable<string> StringsOf(CustomAttributeData attribute) =>
        attribute.ConstructorArguments.Count == 1 && attribute.ConstructorArguments[0].Value is IEnumerable<CustomAttributeTypedArgument> values
            ? values.Select(_ => _.Value).OfType<string>()
            : [];

    static bool HasAttribute(Type type, string attributeName) =>
        type.GetCustomAttributesData().Any(_ => _.AttributeType.Name == attributeName);

    static IEnumerable<ManifestNamedCondition> NamedConditions(ManifestExpression expression) => expression switch
    {
        ManifestWhen { Condition: ManifestNamedCondition named } => [named],
        ManifestAll all => all.Items.SelectMany(NamedConditions),
        ManifestAny any => any.Items.SelectMany(NamedConditions),
        _ => []
    };
}
