// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Availability;

/// <summary>
/// Builds the <c>provide</c> lines of the request-scoped values the code resolves from read model queries and places
/// them at the level of the model they are declared at.
/// </summary>
/// <param name="naming">The naming to apply to the names the code uses.</param>
/// <param name="types">The converter of the types a value is declared with.</param>
/// <param name="diagnostics">Where what cannot be stated is reported.</param>
public class ContextValueSyntaxBuilder(IScreenplayNaming naming, TypeReferenceConverter types, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Places the values of the model.
    /// </summary>
    /// <param name="model">The application model carrying the values and the queries that resolve them.</param>
    /// <param name="modules">The modules built for the application.</param>
    /// <returns>The values provided at the application, and the modules with the values provided at them.</returns>
    public (IReadOnlyList<ProvideSyntax> Application, IReadOnlyList<ModuleSyntax> Modules) Place(ApplicationModel model, IReadOnlyList<ModuleSyntax> modules)
    {
        var queries = model.Slices.SelectMany(_ => _.Queries).ToList();
        var application = new List<ProvideSyntax>();
        var placed = modules;

        foreach (var value in model.ContextValues.OrderBy(_ => _.Name, StringComparer.Ordinal))
        {
            var provide = Provide(value, queries);
            if (provide is null)
            {
                continue;
            }

            if (value.Scope.Count == 0)
            {
                application.Add(provide);
                continue;
            }

            var modified = Declare(placed, value.Scope, provide) ?? DeclareBeneathAModule(placed, value.Scope, provide);
            if (modified is null)
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.AvailabilityNotRepresentable,
                    $"The context value '{value.Name}' is declared at the scope '{string.Join('.', value.Scope)}', which names no module, feature or slice of the generated model, and was left out",
                    value.Location);
                continue;
            }

            placed = modified;
        }

        return (application, placed);
    }

    /// <summary>
    /// Declares a value at the first feature or slice the scope names beneath a module, for a scope that does not start at a module.
    /// </summary>
    /// <param name="modules">The modules to look in.</param>
    /// <param name="scope">The names the value is declared at.</param>
    /// <param name="provide">The line to declare.</param>
    /// <returns>The modules with the line declared, or <see langword="null"/> when the scope names no part of them.</returns>
    /// <remarks>
    /// Namespaces become features beneath the module named after the project, and a scope is the name the application gave
    /// a part of its namespace, so a scope that starts at a feature is read from the module down.
    /// </remarks>
    static List<ModuleSyntax>? DeclareBeneathAModule(IReadOnlyList<ModuleSyntax> modules, IReadOnlyList<string> scope, ProvideSyntax provide)
    {
        for (var index = 0; index < modules.Count; index++)
        {
            if (Declare(modules[index].Features, [.. scope], provide) is { } features)
            {
                var result = modules.ToList();
                result[index] = modules[index] with { Features = features };

                return result;
            }
        }

        return null;
    }

    static List<ModuleSyntax>? Declare(IReadOnlyList<ModuleSyntax> modules, IReadOnlyList<string> scope, ProvideSyntax provide)
    {
        var index = modules.ToList().FindIndex(_ => _.Name == scope[0]);
        if (index < 0)
        {
            return null;
        }

        var module = modules[index];
        ModuleSyntax? declared;
        if (scope.Count == 1)
        {
            declared = module with { Provides = [.. module.Provides, provide] };
        }
        else if (Declare(module.Features, [.. scope.Skip(1)], provide) is { } features)
        {
            declared = module with { Features = features };
        }
        else
        {
            return null;
        }

        var result = modules.ToList();
        result[index] = declared;

        return result;
    }

    static List<FeatureSyntax>? Declare(IEnumerable<FeatureSyntax> features, List<string> scope, ProvideSyntax provide)
    {
        var all = features.ToList();
        var index = all.FindIndex(_ => _.Name == scope[0]);
        if (index < 0)
        {
            return null;
        }

        var feature = all[index];
        if (scope.Count == 1)
        {
            all[index] = feature with { Provides = [.. feature.Provides, provide] };
            return all;
        }

        var children = Declare(feature.Features, [.. scope.Skip(1)], provide);
        if (children is not null)
        {
            all[index] = feature with { Features = children };
            return all;
        }

        var slices = feature.Slices.ToList();
        var slice = slices.FindIndex(_ => _.Name == scope[1]);
        if (scope.Count != 2 || slice < 0)
        {
            return null;
        }

        slices[slice] = slices[slice] with { Provides = [.. slices[slice].Provides, provide] };
        all[index] = feature with { Slices = slices };

        return all;
    }

    ProvideSyntax? Provide(ContextValueModel value, List<QueryModel> queries)
    {
        var matching = queries.Where(_ => _.Name == value.Query).ToList();
        if (matching.Count != 1 || matching[0].IsObservable || matching[0].By is null)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.AvailabilityNotRepresentable,
                $"The context value '{value.Name}' is resolved by the query '{value.Query}', which is not exactly one keyed snapshot query of the application, and was left out",
                value.Location);
            return null;
        }

        var type = types.Convert(matching[0].ReturnType);
        if (type.IsCollection)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.AvailabilityNotRepresentable,
                $"The context value '{value.Name}' is resolved by the query '{value.Query}', which returns a collection, and was left out",
                value.Location);
            return null;
        }

        return new(
            naming.ToPropertyName(value.Name),
            type with { IsOptional = type.IsOptional || !value.Required },
            new QueryProvideSourceSyntax(
                naming.ToDeclarationName(value.Query),
                [new ContextExpressionSyntax(value.Key, SourceLocation.Start)],
                SourceLocation.Start),
            SourceLocation.Start);
    }
}
