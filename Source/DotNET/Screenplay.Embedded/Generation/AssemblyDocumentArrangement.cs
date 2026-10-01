// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Arranges the assembly document with the same module boundaries as its navigation catalog.
/// </summary>
/// <remarks>
/// The compatibility emitter uses one module for an assembly. Here, identified module features are lifted into
/// actual modules. Rooted features keep the root container required by Screenplay's grammar, without becoming
/// modules in the catalog. The already-built syntax is reused so artifact names and cross-scope references agree.
/// </remarks>
public static class AssemblyDocumentArrangement
{
    /// <summary>
    /// Arranges a root emission according to the recovered scopes.
    /// </summary>
    /// <param name="emission">The complete assembly emission.</param>
    /// <param name="scopes">The scopes identified from the application's namespaces.</param>
    /// <returns>The arranged emission.</returns>
    public static ScreenplayEmission Apply(ScreenplayEmission emission, IEnumerable<DocumentScope> scopes)
    {
        var naming = new ScreenplayNaming();
        var names = scopes.Where(_ => _.Kind == EmbeddedDocumentKind.Module)
            .Select(_ => naming.ToDeclarationName(_.Title)).ToHashSet(StringComparer.Ordinal);
        var modules = emission.Application.Modules.SelectMany(_ => LiftModules(_, names));
        var application = emission.Application with { Modules = MergeModules(modules) };

        return emission with { Application = application, Source = new ScreenplayPrinter().Print(application) };
    }

    static IEnumerable<ModuleSyntax> LiftModules(ModuleSyntax root, HashSet<string> moduleNames)
    {
        var rooted = new List<FeatureSyntax>();
        foreach (var feature in root.Features)
        {
            if (moduleNames.Contains(feature.Name))
            {
                yield return root with { Name = feature.Name, Features = ModuleFeatures(feature) };
            }
            else
            {
                rooted.Add(feature);
            }
        }

        if (rooted.Count > 0)
        {
            yield return root with { Features = rooted };
        }
    }

    static IEnumerable<FeatureSyntax> ModuleFeatures(FeatureSyntax feature) =>
        feature.Slices.Any()
            ? [feature with { Features = [] }, .. feature.Features]
            : feature.Features;

    static IEnumerable<ModuleSyntax> MergeModules(IEnumerable<ModuleSyntax> modules) =>
        modules.GroupBy(_ => _.Name, StringComparer.Ordinal)
            .OrderBy(_ => _.Key, StringComparer.Ordinal)
            .Select(group => group.First() with
            {
                Features = [.. group.SelectMany(_ => _.Features).OrderBy(_ => _.Name, StringComparer.Ordinal)]
            });
}
