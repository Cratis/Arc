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
/// demand, so roles are combined with <c>or</c> and policies follow them without one.
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

        return new(
            [
                .. roles.Select((role, index) => new PolicyReferenceSyntax(role, index > 0, SourceLocation.Start)),
                .. named.Select(policy => new PolicyReferenceSyntax(policy, false, SourceLocation.Start))
            ],
            SourceLocation.Start);
    }

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
