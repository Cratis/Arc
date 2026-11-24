// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.Filters;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands;

/// <summary>Identifies command filter behavior that standalone authorization cannot reproduce.</summary>
static class CommandAuthorizationFilters
{
    /// <summary>Inspects the same scoped discovered filters used by the command pipeline, without executing them.</summary>
    /// <param name="type">The command type.</param>
    /// <param name="services">The application scope.</param>
    /// <returns>An unsupported diagnostic, or null when the filters permit standalone evaluation.</returns>
    internal static CommandAuthorizationResult? Inspect(Type type, IServiceProvider services)
    {
        var filters = services.GetService<IInstancesOf<ICommandFilter>>();
        if (filters is null) return null;

        foreach (var filter in filters)
        {
            var filterType = filter.GetType();
            if (filterType == typeof(AvailabilityFilter))
            {
                if (services.GetService<IAvailabilityGate>()?.IsGated(type) == true)
                {
                    return CommandAuthorizationResult.Unsupported(type, "availability-gate", "Use a command scenario to evaluate availability for a gated command.");
                }
            }
            else if (!IsBuiltIn(filterType))
            {
                return CommandAuthorizationResult.Unsupported(type, "custom-filter", "Use a command scenario to evaluate custom command filters, including ordinary filters.");
            }
        }

        return null;
    }

    static bool IsBuiltIn(Type type) => type == typeof(AuthorizationFilter) ||
        type == typeof(DataAnnotationValidationFilter) || type == typeof(FluentValidationFilter) || IsChronicleValidationFilter(type);

    /// <summary>Identifies Chronicle's audited validation-only filter without adding an optional package dependency.</summary>
    /// <remarks>Does not trust application subclasses, matching namespaces or an assembly-name prefix.</remarks>
    /// <param name="type">The exact filter type.</param>
    /// <returns>Whether it is the built-in Chronicle filter.</returns>
    static bool IsChronicleValidationFilter(Type type) =>
        type.FullName == "Cratis.Arc.Chronicle.Commands.RequiredConceptFilter" && type.Assembly.GetName().Name == "Cratis.Arc.Chronicle";
}
