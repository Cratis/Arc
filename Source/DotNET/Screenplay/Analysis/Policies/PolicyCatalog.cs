// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Policies;

/// <summary>
/// Declares a policy for everything the application asks of its callers.
/// </summary>
/// <param name="compilation">The compilation being analyzed.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unrecoverable is reported to.</param>
/// <remarks>
/// Only what something refers to is declared. A role named by an artifact is a policy whose rule is the role itself;
/// a policy named by an artifact has its rule looked up where the application registers it. Declaring the ones that
/// nothing refers to would fill the document with rules it never uses.
/// </remarks>
public class PolicyCatalog(Compilation compilation, ScreenplayDiagnostics diagnostics)
{
    readonly PolicyRequirementReader _requirements = new(diagnostics);

    /// <summary>
    /// Declares a policy for every role and every named policy the slices refer to.
    /// </summary>
    /// <param name="authorizations">Everything within the application that requires something of the caller.</param>
    /// <returns>The policies, ordered by name.</returns>
    public IEnumerable<PolicyModel> Declare(IEnumerable<AuthorizationModel> authorizations)
    {
        var declared = authorizations as IReadOnlyCollection<AuthorizationModel> ?? [.. authorizations];
        var registrations = PolicyRegistrations.In(compilation);
        var policies = new Dictionary<string, PolicyModel>(StringComparer.Ordinal);

        foreach (var name in Names(declared, _ => _.Policies))
        {
            policies[name] = new(name, Requirement(name, registrations));
        }

        foreach (var role in Names(declared, _ => _.Roles).Where(_ => !policies.ContainsKey(_)))
        {
            policies[role] = PolicyModel.ForRole(role);
        }

        return [.. policies.Values.OrderBy(_ => _.Name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Gets the names one part of an authorization refers to.
    /// </summary>
    /// <param name="authorizations">The authorizations to read.</param>
    /// <param name="select">The part to read from each of them.</param>
    /// <returns>The names, distinct and ordered.</returns>
    static IEnumerable<string> Names(
        IEnumerable<AuthorizationModel> authorizations,
        Func<AuthorizationModel, IEnumerable<string>> select) =>
        authorizations
            .SelectMany(select)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);

    /// <summary>
    /// Looks up what a named policy requires, reporting one that the application never registers.
    /// </summary>
    /// <param name="name">The name of the policy.</param>
    /// <param name="registrations">Every policy the application registers.</param>
    /// <returns>The requirement, or <see langword="null"/> when it could not be recovered.</returns>
    PolicyRequirementModel? Requirement(string name, IReadOnlyDictionary<string, PolicyRegistration> registrations)
    {
        if (registrations.TryGetValue(name, out var registration))
        {
            return _requirements.Read(registration);
        }

        diagnostics.Warning(
            ScreenplayDiagnosticCodes.PolicyRequirementsUnrecoverable,
            $"The policy '{name}' is referred to, but nothing in the compilation registers it, so what it requires is not stated",
            compilation.AssemblyName);

        return null;
    }
}
