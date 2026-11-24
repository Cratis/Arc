// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Availability;

/// <summary>
/// Places something on the module, feature or slice a scope names.
/// </summary>
/// <remarks>
/// A scope is the names a part of a namespace was given. Namespaces become features beneath the module named after the project,
/// so a scope that starts at a feature is read from the module down, and a scope that starts at the namespace root is tried
/// again without its leading names until one of them names a part of the generated model.
/// </remarks>
/// <param name="OnModule">Changes a module.</param>
/// <param name="OnFeature">Changes a feature.</param>
/// <param name="OnSlice">Changes a slice.</param>
public record ScopePlacement(
    Func<ModuleSyntax, ModuleSyntax> OnModule,
    Func<FeatureSyntax, FeatureSyntax> OnFeature,
    Func<SliceSyntax, SliceSyntax> OnSlice)
{
    /// <summary>
    /// Places a change at the part a scope names.
    /// </summary>
    /// <param name="modules">The modules to look in.</param>
    /// <param name="scope">The names the change is placed at.</param>
    /// <param name="trimLeading">Whether to try the scope again without its leading names, for a scope that is a whole namespace.</param>
    /// <returns>The modules with the change, or <see langword="null"/> when the scope names no part of them.</returns>
    public IReadOnlyList<ModuleSyntax>? Place(IReadOnlyList<ModuleSyntax> modules, IReadOnlyList<string> scope, bool trimLeading = false)
    {
        for (var skip = 0; skip < (trimLeading ? scope.Count : 1); skip++)
        {
            var names = scope.Skip(skip).ToList();
            var placed = Declare(modules, names) ?? DeclareBeneathAModule(modules, names);
            if (placed is not null)
            {
                return placed;
            }
        }

        return null;
    }

    List<ModuleSyntax>? DeclareBeneathAModule(IReadOnlyList<ModuleSyntax> modules, List<string> scope)
    {
        for (var index = 0; index < modules.Count; index++)
        {
            if (Declare(modules[index].Features, [.. scope]) is { } features)
            {
                var result = modules.ToList();
                result[index] = modules[index] with { Features = features };

                return result;
            }
        }

        return null;
    }

    List<ModuleSyntax>? Declare(IReadOnlyList<ModuleSyntax> modules, List<string> scope)
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
            declared = OnModule(module);
        }
        else if (Declare(module.Features, [.. scope.Skip(1)]) is { } features)
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

    List<FeatureSyntax>? Declare(IEnumerable<FeatureSyntax> features, List<string> scope)
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
            all[index] = OnFeature(feature);
            return all;
        }

        var children = Declare(feature.Features, [.. scope.Skip(1)]);
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

        slices[slice] = OnSlice(slices[slice]);
        all[index] = feature with { Slices = slices };

        return all;
    }
}
