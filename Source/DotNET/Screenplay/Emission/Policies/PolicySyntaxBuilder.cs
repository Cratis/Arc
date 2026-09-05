// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Policies;

/// <summary>
/// Builds the document level <c>policy</c> declarations.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
public class PolicySyntaxBuilder(IScreenplayNaming naming)
{
    readonly PolicyConditionConverter _conditions = new(naming);

    /// <summary>
    /// Builds every policy the document declares.
    /// </summary>
    /// <param name="declared">The policies the model declares.</param>
    /// <param name="referenced">The policies the emitted authorize blocks reference.</param>
    /// <returns>The policy declarations, ordered by name.</returns>
    /// <remarks>
    /// A policy referenced by an artifact but not declared by the model is synthesized as a role policy, so that the
    /// document is always self consistent. The one exception is the well known authenticated policy, which requires
    /// nothing but a caller.
    /// </remarks>
    public IEnumerable<PolicySyntax> Build(IEnumerable<PolicyModel> declared, IEnumerable<string> referenced)
    {
        var policies = new Dictionary<string, PolicySyntax>(StringComparer.Ordinal);

        foreach (var policy in declared)
        {
            var name = PolicyNames.For(policy.Name);
            if (name.Length == 0 || policies.ContainsKey(name))
            {
                continue;
            }

            policies[name] = new(name, _conditions.Convert(policy.Requirement), null, SourceLocation.Start);
        }

        foreach (var name in referenced.Where(_ => !policies.ContainsKey(_)))
        {
            policies[name] = new(name, Synthesize(name), null, SourceLocation.Start);
        }

        return [.. policies.Values.OrderBy(_ => _.Name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Synthesizes the condition of a policy that is referenced but never declared.
    /// </summary>
    /// <param name="name">The name of the policy.</param>
    /// <returns>The condition.</returns>
    static PolicyConditionSyntax Synthesize(string name) =>
        string.Equals(name, AuthorizeSyntaxBuilder.AuthenticatedPolicy, StringComparison.Ordinal)
            ? new AuthenticatedConditionSyntax(SourceLocation.Start)
            : new RoleConditionSyntax(name, SourceLocation.Start);
}
