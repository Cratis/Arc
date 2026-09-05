// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Policies;

/// <summary>
/// Builds the Screenplay <c>authorize</c> block for a command or a query, recording every policy it references.
/// </summary>
/// <remarks>
/// The references are recorded so that the document can declare every policy it uses. A document referencing a
/// policy it never declares still compiles, but it compiles with a warning, and a generated document that warns is
/// a generated document nobody trusts.
/// <para>
/// Roles are alternatives to each other - holding any one of them is enough - while a named policy is an additional
/// demand, so roles are combined with <c>or</c> and the policies with <c>and</c>.
/// </para>
/// </remarks>
public class AuthorizeSyntaxBuilder
{
    /// <summary>
    /// The name of the policy requiring nothing but an authenticated caller.
    /// </summary>
    public const string AuthenticatedPolicy = "Authenticated";

    readonly HashSet<string> _referenced = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets every policy referenced so far.
    /// </summary>
    public IEnumerable<string> Referenced => _referenced;

    /// <summary>
    /// Builds the authorize block for what an artifact requires of the caller.
    /// </summary>
    /// <param name="authorization">What the artifact requires, if anything.</param>
    /// <returns>The <see cref="AuthorizeSyntax"/>, or <see langword="null"/> when nothing is required.</returns>
    public AuthorizeSyntax? Build(AuthorizationModel? authorization)
    {
        if (authorization?.RequiresAuthentication != true)
        {
            return null;
        }

        var roles = Named(authorization.Roles);
        var named = Named(authorization.Policies).Where(_ => !roles.Contains(_)).ToList();

        if (roles.Count == 0 && named.Count == 0)
        {
            named.Add(AuthenticatedPolicy);
        }

        foreach (var policy in roles.Concat(named))
        {
            _referenced.Add(policy);
        }

        return new(Require(roles, named), SourceLocation.Start);
    }

    /// <summary>
    /// Combines the roles and the named policies into the single requirement the caller has to satisfy.
    /// </summary>
    /// <param name="roles">The roles, any one of which is enough.</param>
    /// <param name="named">The named policies, every one of which is demanded.</param>
    /// <returns>The <see cref="PolicyRequirementSyntax"/>.</returns>
    /// <remarks>
    /// The two groups are joined into a tree rather than laid out in a list, because <c>and</c> binds tighter than
    /// <c>or</c>. "Any librarian, who is also senior staff" written flat reads back as "any librarian, or anyone who
    /// is senior staff", which admits a caller neither the roles nor the policy admits on its own. Nesting the
    /// alternatives under the demand says what was meant, and the printer parenthesizes it because the operators
    /// differ.
    /// </remarks>
    static PolicyRequirementSyntax Require(IEnumerable<string> roles, IEnumerable<string> named)
    {
        var anyRole = Combine(roles, LogicalOperator.Or);
        var everyPolicy = Combine(named, LogicalOperator.And);

        if (anyRole is not null && everyPolicy is not null)
        {
            return new LogicalPolicyRequirementSyntax(anyRole, LogicalOperator.And, everyPolicy, SourceLocation.Start);
        }

        // Build never leaves both groups empty - it stands in with the authenticated policy - so one of them is here.
        return anyRole ?? everyPolicy ?? new PolicyReferenceSyntax(AuthenticatedPolicy, SourceLocation.Start);
    }

    /// <summary>
    /// Folds names into one requirement combined with a single operator.
    /// </summary>
    /// <param name="names">The names to fold.</param>
    /// <param name="operator">The <see cref="LogicalOperator"/> combining them.</param>
    /// <returns>The requirement, or <see langword="null"/> when there are no names to fold.</returns>
    static PolicyRequirementSyntax? Combine(IEnumerable<string> names, LogicalOperator @operator) =>
        names
            .Select(name => (PolicyRequirementSyntax)new PolicyReferenceSyntax(name, SourceLocation.Start))
            .Aggregate(
                default(PolicyRequirementSyntax),
                (left, right) => left is null
                    ? right
                    : new LogicalPolicyRequirementSyntax(left, @operator, right, SourceLocation.Start));

    /// <summary>
    /// Converts names into the form a policy reference takes, leaving out anything nothing is left of.
    /// </summary>
    /// <param name="names">The names to convert.</param>
    /// <returns>The names, distinct and ordered.</returns>
    static List<string> Named(IEnumerable<string> names) =>
    [
        .. names
            .Select(PolicyNames.For)
            .Where(_ => _.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
    ];
}
