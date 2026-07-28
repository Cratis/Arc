// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Events;

/// <summary>
/// Resolves the events an application refers to but does not declare itself.
/// </summary>
/// <remarks>
/// A reactor observing what a sibling bounded context publishes refers to an event living in a referenced package.
/// The event is real and the document has to name it, but nothing in the compilation declares it, so a document
/// stating only the reference refers to something it never introduces. Screenplay has the construct for exactly this
/// situation - an <c>import</c> states the dependency outright and registers the name as an event that is known - so
/// the one that can be found is imported and only the one that cannot is reported.
/// </remarks>
public static class ExternalEvents
{
    /// <summary>
    /// Resolves the events the application refers to without declaring them, reporting every one nothing declares.
    /// </summary>
    /// <param name="compilation">The compilation being analyzed.</param>
    /// <param name="slices">The slices to read.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <returns>The fully qualified name of every event to import, ordered.</returns>
    public static IReadOnlyList<string> Resolve(
        Compilation compilation,
        IReadOnlyList<SliceModel> slices,
        ScreenplayDiagnostics diagnostics)
    {
        var declared = slices.SelectMany(_ => _.Events).Select(_ => _.Name).ToHashSet(StringComparer.Ordinal);
        var undeclared = slices.SelectMany(ReferredToBy).Where(_ => !declared.Contains(_)).ToHashSet(StringComparer.Ordinal);
        var imported = DeclaredByAReference(compilation, undeclared);

        foreach (var slice in slices)
        {
            foreach (var name in ReferredToBy(slice)
                .Where(_ => !declared.Contains(_) && !imported.ContainsKey(_))
                .Order(StringComparer.Ordinal))
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.EventDeclaredOutsideCompilation,
                    $"'{name}' is referred to but nothing declares it, so the document refers to an event it never introduces",
                    slice.Namespace);
            }
        }

        return [.. imported.Values.Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Gets the names of every event a slice refers to.
    /// </summary>
    /// <param name="slice">The slice to read.</param>
    /// <returns>The names, distinct.</returns>
    public static IEnumerable<string> ReferredToBy(SliceModel slice) =>
        slice.Commands.SelectMany(_ => _.Produces).Select(_ => _.EventName)
            .Concat(slice.Reactors.SelectMany(_ => _.ObservedEvents))
            .Concat(slice.Constraints.SelectMany(EventsOf))
            .Concat(ProjectionEvents.In(slice.Projection))
            .Distinct(StringComparer.Ordinal);

    /// <summary>
    /// Gets the names of the events a constraint refers to.
    /// </summary>
    /// <param name="constraint">The constraint to read.</param>
    /// <returns>The names.</returns>
    static IEnumerable<string> EventsOf(ConstraintModel constraint) => constraint switch
    {
        UniquePropertyConstraintModel unique => [unique.EventName],
        UniqueEventConstraintModel unique => [unique.EventName],
        _ => []
    };

    /// <summary>
    /// Finds the event a referenced assembly declares under each of a set of names.
    /// </summary>
    /// <param name="compilation">The compilation being analyzed.</param>
    /// <param name="names">The names to look for.</param>
    /// <returns>The fully qualified name each one was found under, keyed by the name it is referred to by.</returns>
    /// <remarks>
    /// Every assembly says which type names it holds without any of them being read, so the search only ever opens
    /// the few that could answer. Assemblies and namespaces are both walked in name order and the first declaration
    /// of a name wins, because two packages declaring an event under one name is a document that would otherwise
    /// depend on the order the compiler happened to hand its references over.
    /// </remarks>
    static Dictionary<string, string> DeclaredByAReference(Compilation compilation, HashSet<string> names)
    {
        var found = new Dictionary<string, string>(StringComparer.Ordinal);
        if (names.Count == 0)
        {
            return found;
        }

        foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols
            .OrderBy(_ => _.Identity.GetDisplayName(), StringComparer.Ordinal))
        {
            if (names.Any(assembly.TypeNames.Contains))
            {
                Collect(assembly.GlobalNamespace, names, found);
            }
        }

        return found;
    }

    /// <summary>
    /// Collects every event declared under one of a set of names within a namespace and those nested in it.
    /// </summary>
    /// <param name="namespace">The namespace to walk.</param>
    /// <param name="names">The names to look for.</param>
    /// <param name="found">The declarations found so far.</param>
    static void Collect(INamespaceSymbol @namespace, HashSet<string> names, Dictionary<string, string> found)
    {
        foreach (var name in names.Where(_ => !found.ContainsKey(_)).Order(StringComparer.Ordinal))
        {
            var declaration = @namespace.GetTypeMembers(name)
                .Where(EventReader.IsEvent)
                .OrderBy(_ => _.ToDisplayString(), StringComparer.Ordinal)
                .FirstOrDefault();

            if (declaration is not null)
            {
                found[name] = declaration.ToDisplayString();
            }
        }

        foreach (var nested in @namespace.GetNamespaceMembers().OrderBy(_ => _.Name, StringComparer.Ordinal))
        {
            Collect(nested, names, found);
        }
    }
}
