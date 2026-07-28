// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Policies;

/// <summary>
/// Builds the Screenplay <c>authorize</c> block for a command or a query, recording every policy it references.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <remarks>
/// The references are recorded so that the document can declare every policy it uses. A document referencing a
/// policy it never declares still compiles, but it compiles with a warning, and a generated document that warns is
/// a generated document nobody trusts.
/// </remarks>
public class AuthorizeSyntaxBuilder(IScreenplayNaming naming)
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

        var policies = authorization.Roles
            .Select(naming.ToDeclarationName)
            .Where(_ => _.Length > 1)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        if (policies.Count == 0)
        {
            policies.Add(AuthenticatedPolicy);
        }

        foreach (var policy in policies)
        {
            _referenced.Add(policy);
        }

        return new(
            [.. policies.Select((policy, index) => new PolicyReferenceSyntax(policy, index > 0, SourceLocation.Start))],
            SourceLocation.Start);
    }
}
