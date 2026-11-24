// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Emission.Policies;

/// <summary>
/// Refuses anonymous authorization that cannot be expressed without weakening another caller requirement.
/// </summary>
internal static class AnonymousPolicyAdmission
{
    /// <summary>
    /// Checks declarations and operation gates before any executable syntax is constructed.
    /// </summary>
    /// <param name="model">The original candidate model.</param>
    /// <returns>Explicit refusal errors, leaving the model unchanged.</returns>
    internal static IReadOnlyList<ScreenplayDiagnostic> Check(ApplicationModel model)
    {
        var diagnostics = new List<ScreenplayDiagnostic>();
        var anonymous = Declarations(model, diagnostics);
        if (anonymous.Count == 0)
        {
            return DistinctRefusals(diagnostics);
        }

        foreach (var slice in model.Slices)
        {
            foreach (var command in slice.Commands)
            {
                CheckOperation(command.Authorization, anonymous, $"command '{command.Name}'", slice.Namespace, diagnostics);
            }

            foreach (var query in slice.Queries)
            {
                CheckOperation(query.Authorization, anonymous, $"query '{query.Name}'", slice.Namespace, diagnostics);
            }
        }

        return DistinctRefusals(diagnostics);
    }

    /// <summary>
    /// Identifies only the explicit anonymous-policy refusal errors, not unrelated warnings or errors.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to inspect.</param>
    /// <returns>Whether the candidate must not be emitted or verified.</returns>
    internal static bool IsRefusal(ScreenplayDiagnostic diagnostic) =>
        diagnostic.Severity == ScreenplayDiagnosticSeverity.Error &&
        (diagnostic.Code == ScreenplayDiagnosticCodes.AnonymousAuthorizationNotRepresentable ||
         diagnostic.Code == ScreenplayDiagnosticCodes.AmbiguousAnonymousPolicyRegistration);

    /// <summary>
    /// Deduplicates refusal identities without changing unrelated diagnostics.
    /// </summary>
    /// <param name="diagnostics">The combined diagnostics in reporting order.</param>
    /// <returns>The diagnostics with each refusal code, location and message reported once.</returns>
    internal static IReadOnlyList<ScreenplayDiagnostic> DistinctRefusals(IEnumerable<ScreenplayDiagnostic> diagnostics)
    {
        var seen = new HashSet<(string Code, string? Location, string Message)>();
        return [.. diagnostics.Where(diagnostic => !IsRefusal(diagnostic) || seen.Add((diagnostic.Code, diagnostic.Location, diagnostic.Message)))];
    }

    static HashSet<string> Declarations(ApplicationModel model, List<ScreenplayDiagnostic> diagnostics)
    {
        foreach (var policy in model.Policies)
        {
            var refusal = PolicyRecovery.RefusalFor(policy);
            if (refusal is not null)
            {
                diagnostics.Add(refusal);
            }

            if (ContainsAnonymous(policy.Requirement) && policy.Requirement is not AnonymousRequirement)
            {
                diagnostics.Add(Unrepresentable($"policy '{policy.Name}'", "an anonymous condition combined with another condition", model.Domain));
            }
        }

        var anonymous = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in model.Policies.GroupBy(policy => PolicyNames.For(policy.Name), StringComparer.OrdinalIgnoreCase))
        {
            if (!group.Any(policy => ContainsAnonymous(policy.Requirement)))
            {
                continue;
            }

            anonymous.Add(group.Key);
            CheckNames(group, model.Domain, diagnostics);
        }

        return anonymous;
    }

    static void CheckNames(IGrouping<string, PolicyModel> declarations, string location, List<ScreenplayDiagnostic> diagnostics)
    {
        if (declarations.Key.Length > 0 && declarations.Count() == 1)
        {
            return;
        }

        var names = string.Join(", ", declarations.Select(policy => $"'{policy.Name}'"));
        diagnostics.Add(new(
            ScreenplayDiagnosticSeverity.Error,
            ScreenplayDiagnosticCodes.AmbiguousAnonymousPolicyRegistration,
            $"The anonymous policy declarations {names} do not have distinct, representable policy names. No executable document was emitted",
            location));
    }

    static bool ContainsAnonymous(PolicyRequirementModel? requirement) => requirement switch
    {
        AnonymousRequirement => true,
        CombinedRequirement combined => ContainsAnonymous(combined.Left) || ContainsAnonymous(combined.Right),
        _ => false
    };

    static void CheckOperation(
        AuthorizationModel? authorization,
        HashSet<string> anonymous,
        string operation,
        string location,
        List<ScreenplayDiagnostic> diagnostics)
    {
        if (authorization is null)
        {
            return;
        }

        var names = authorization.Policies.Concat(authorization.Roles)
            .Where(name => anonymous.Contains(PolicyNames.For(name))).Distinct(StringComparer.Ordinal).ToArray();
        if (names.Length == 0)
        {
            return;
        }

        var reason = Restriction(authorization);
        if (reason is not null)
        {
            diagnostics.Add(Unrepresentable($"{operation} using anonymous policy {string.Join(", ", names.Select(name => $"'{name}'"))}", reason, location));
        }
    }

    static string? Restriction(AuthorizationModel authorization)
    {
        if (authorization.RequiresAuthentication)
        {
            return "an authentication requirement";
        }

        if (authorization.Roles.Any())
        {
            return "role requirements";
        }

        return authorization.Policies.Distinct(StringComparer.Ordinal).Count() == 1
            ? null
            : "additional distinct policy requirements";
    }

    static ScreenplayDiagnostic Unrepresentable(string subject, string reason, string location) =>
        new(
            ScreenplayDiagnosticSeverity.Error,
            ScreenplayDiagnosticCodes.AnonymousAuthorizationNotRepresentable,
            $"The {subject} has {reason}, which cannot be represented faithfully with an anonymous gate. No executable document was emitted",
            location);
}
