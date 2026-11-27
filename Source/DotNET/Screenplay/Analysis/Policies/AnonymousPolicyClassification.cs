// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Policies;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Policies;

/// <summary>
/// Proves which native named policies unconditionally admit callers without authentication.
/// </summary>
/// <param name="compilations">Every project in the analyzed application.</param>
internal sealed class AnonymousPolicyClassification(IReadOnlyList<Compilation> compilations)
{
    readonly RegistrationProof _proof = Classify(compilations);

    /// <summary>
    /// Determines whether every authorization attribute names only a proven unconditional anonymous policy.
    /// </summary>
    /// <param name="attributes">The artifact's complete set of authorization attributes.</param>
    /// <returns>Whether a nonempty set of requirements is proven to admit anonymous callers.</returns>
    internal bool Allows(IReadOnlyList<AttributeData> attributes) =>
        attributes.Count > 0 && attributes.All(IsAnonymousPolicyOnly);

    /// <summary>
    /// Gets registration ambiguity that prevents an anonymous policy from being emitted faithfully.
    /// </summary>
    /// <param name="name">The declared policy name.</param>
    /// <returns>The ambiguity reason, or null when no anonymous registration ambiguity was found.</returns>
    internal string? RefusalFor(string name) => _proof.Refusals.GetValueOrDefault(PolicyNames.For(name));

    static RegistrationProof Classify(IReadOnlyList<Compilation> compilations)
    {
        var registrations = PolicyRegistrations.All(compilations).ToArray();
        var unknown = registrations.FirstOrDefault(registration => registration.Name.Length == 0);
        var reader = new ArcPolicyConditionReader(compilations);
        var proof = new RegistrationProof(new(StringComparer.Ordinal), new(StringComparer.OrdinalIgnoreCase));

        foreach (var group in registrations.GroupBy(registration => PolicyNames.For(registration.Name), StringComparer.OrdinalIgnoreCase))
        {
            var candidates = group.OfType<ArcPolicyRegistration>()
                .Where(registration => registration.EvaluatesAnonymous && reader.Read(registration).Requirement is AnonymousRequirement)
                .ToArray();
            if (candidates.Length == 0)
            {
                continue;
            }

            if (unknown is not null || group.Count() != 1)
            {
                proof.Refusals[group.Key] = Ambiguity(group, unknown);
            }
            else if (group.Key.Length > 0)
            {
                proof.Names.Add(candidates[0].Name);
            }
        }

        return proof;
    }

    static string Ambiguity(IEnumerable<PolicyRegistration> registrations, PolicyRegistration? unknown) =>
        unknown is not null
            ? $"a registration at '{unknown.Location}' has an unresolved name and may overlap this anonymous policy"
            : $"multiple registrations normalize to the same policy name at {string.Join(", ", registrations.Select(registration => $"'{registration.Location}'").Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))}";

    bool IsAnonymousPolicyOnly(AttributeData attribute) =>
        attribute.AttributeClass.Is(WellKnownTypeNames.AuthorizeAttribute) &&
        string.IsNullOrEmpty(attribute.GetNamedArgument(AuthorizationReader.RolesProperty) as string) &&
        string.IsNullOrEmpty(attribute.GetNamedArgument("AuthenticationSchemes") as string) &&
        attribute.GetNamedArgument(AuthorizationReader.PolicyProperty) is string name && _proof.Names.Contains(name.Trim());

    sealed record RegistrationProof(HashSet<string> Names, Dictionary<string, string> Refusals);
}
