// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Policies;

/// <summary>
/// Converts what a policy requires into the condition expressing it.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
public class PolicyConditionConverter(IScreenplayNaming naming)
{
    /// <summary>
    /// Converts a requirement into the condition expressing it.
    /// </summary>
    /// <param name="requirement">The requirement to convert, if any.</param>
    /// <returns>The condition, never <see langword="null"/>.</returns>
    /// <remarks>
    /// A policy has to require something - the grammar has no empty policy - so a requirement that was never
    /// recovered becomes the least the reference itself implies, which is that somebody is there at all. The
    /// analysis reports it rather than letting the document present the fallback as what the application says.
    /// </remarks>
    public PolicyConditionSyntax Convert(PolicyRequirementModel? requirement) =>
        Read(requirement) ?? new AuthenticatedConditionSyntax(SourceLocation.Start);

    /// <summary>
    /// Converts a requirement, yielding nothing when nothing of it survives.
    /// </summary>
    /// <param name="requirement">The requirement to convert.</param>
    /// <returns>The condition, or <see langword="null"/>.</returns>
    PolicyConditionSyntax? Read(PolicyRequirementModel? requirement) => requirement switch
    {
        AuthenticatedRequirement => new AuthenticatedConditionSyntax(SourceLocation.Start),
        RoleRequirement role => Role(role.Role),
        ClaimRequirement claim => Claim(claim),
        CombinedRequirement combined => Combine(combined),
        _ => null
    };

    /// <summary>
    /// Converts a role requirement.
    /// </summary>
    /// <param name="role">The name of the role.</param>
    /// <returns>The condition, or <see langword="null"/> when the name cannot be written.</returns>
    RoleConditionSyntax? Role(string role) =>
        naming.ToStringLiteral(role) is { } name ? new RoleConditionSyntax(name, SourceLocation.Start) : null;

    /// <summary>
    /// Converts a claim requirement.
    /// </summary>
    /// <param name="claim">The requirement to convert.</param>
    /// <returns>The condition, or <see langword="null"/> when either part cannot be written.</returns>
    ClaimConditionSyntax? Claim(ClaimRequirement claim) =>
        naming.ToStringLiteral(claim.Claim) is { } name && naming.ToStringLiteral(claim.Value) is { } value
            ? new ClaimConditionSyntax(name, false, value, SourceLocation.Start)
            : null;

    /// <summary>
    /// Converts two combined requirements, keeping whichever side survives on its own.
    /// </summary>
    /// <param name="combined">The requirement to convert.</param>
    /// <returns>The condition, or <see langword="null"/> when neither side survives.</returns>
    PolicyConditionSyntax? Combine(CombinedRequirement combined)
    {
        var left = Read(combined.Left);
        var right = Read(combined.Right);

        if (left is null || right is null)
        {
            return left ?? right;
        }

        return new LogicalPolicyConditionSyntax(
            left,
            combined.IsOr ? LogicalOperator.Or : LogicalOperator.And,
            right,
            SourceLocation.Start);
    }
}
