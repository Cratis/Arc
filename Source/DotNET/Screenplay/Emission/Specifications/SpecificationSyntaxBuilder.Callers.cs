// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Emission.Policies;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

public partial class SpecificationSyntaxBuilder
{
    /// <summary>
    /// Determines whether a command has an authorization gate.
    /// </summary>
    /// <param name="authorization">The command's authorization requirements.</param>
    /// <returns>Whether a gate exists.</returns>
    static bool RequiresAuthorization(AuthorizationModel? authorization) =>
        authorization is not null && HasAuthorizationRequirement(authorization);

    /// <summary>
    /// Determines whether authentication or named policies are required.
    /// </summary>
    /// <param name="authorization">The declared authorization.</param>
    /// <returns>Whether an authorization requirement exists.</returns>
    static bool HasAuthorizationRequirement(AuthorizationModel authorization) =>
        authorization.RequiresAuthentication || HasDeclaredPolicies(authorization);

    /// <summary>
    /// Determines whether roles or named policies are declared.
    /// </summary>
    /// <param name="authorization">The declared authorization.</param>
    /// <returns>Whether roles or named policies exist.</returns>
    static bool HasDeclaredPolicies(AuthorizationModel authorization) => authorization.Roles.Any() || authorization.Policies.Any();

    /// <summary>
    /// States the recovered caller without normalizing away its repeated claims.
    /// </summary>
    /// <param name="caller">The recovered caller.</param>
    /// <returns>The caller syntax, or null when none was recovered.</returns>
    static SpecificationCallerSyntax? Caller(SpecificationCallerModel? caller) => caller is null ? null :
        new(caller.Authenticated, caller.Roles, caller.Claims.Select(claim => new SpecificationCallerClaimSyntax(claim.Key, claim.Value, SourceLocation.Start)), SourceLocation.Start);

    /// <summary>
    /// Checks that a denial has a stated gate, an exact caller, and no other outcome.
    /// </summary>
    /// <param name="specification">The scenario asserting denial.</param>
    /// <param name="command">Its emitted command.</param>
    /// <returns>The reason denial cannot be stated, or null when its evidence is exact.</returns>
    static string? DenialFailure(SpecificationModel specification, CommandModel? command)
    {
        if (specification.Then.Any() || specification.Errors.Any() || specification.AssertsResponse || specification.Returns is not null)
        {
            return "its denial is combined with another outcome";
        }
        if (new AuthorizeSyntaxBuilder().Build(command?.Authorization) is null)
        {
            return "denial not explained by any stated policy";
        }

        return specification.CallerFailure ?? (specification.Caller is null ? "no caller was recovered for its denial" : null);
    }

    /// <summary>
    /// Withholds an unproven authorized scenario without dropping source evidence from open scenarios.
    /// </summary>
    /// <param name="specification">The scenario to filter.</param>
    /// <returns>The unchanged scenario, or null when its authorization cannot be stated.</returns>
    SpecificationModel? WithRepresentableCaller(SpecificationModel specification)
    {
        var command = IssuedCommand(specification);
        var authorized = RequiresAuthorization(command?.Authorization);
        var reason = specification.Denied ? DenialFailure(specification, command) : specification.CallerFailure;
        if (authorized)
        {
            reason ??= specification.Caller is null ? "no caller was recovered for its authorized command" : null;
            var policyFailure = InexactPolicy(command!.Authorization!);
            reason ??= specification.Denied && policyFailure is not null
                ? $"{policyFailure}; denial cannot be decided from the stated caller"
                : policyFailure;
        }

        if (specification.Denied && reason is null)
        {
            reason = new SpecificationDenialProof(naming).Failure(command!.Authorization!, Application!.Policies, Caller(specification.Caller)!);
        }

        if (reason is null)
        {
            return specification;
        }

        var location = SpecificationEvidence.For(specification)?.SourceType.ToDisplayString();
        if (authorized || specification.Denied)
        {
            Diagnostics?.Warning(
                ScreenplayDiagnosticCodes.UnreadableSpecification,
                $"The scenario '{specification.Name}' was left out because {reason}",
                location);
            return null;
        }

        Diagnostics?.Information(
            ScreenplayDiagnosticCodes.SpecificationCallerNotRepresentable,
            $"The scenario '{specification.Name}' was kept without a caller because {reason}",
            location);
        return specification;
    }

    /// <summary>
    /// Finds the first authorization policy whose recovery was incomplete.
    /// </summary>
    /// <param name="authorization">The command's authorization.</param>
    /// <returns>The failure reason, or null when every policy is exact.</returns>
    string? InexactPolicy(AuthorizationModel authorization)
    {
        var policy = authorization.Policies.FirstOrDefault(name => !HasExactPolicy(name));
        if (policy is not null)
        {
            return $"authorization policy '{policy}' was not recovered exactly; a partial or synthesized policy cannot prove the caller outcome";
        }

        var role = authorization.Roles.FirstOrDefault(name => !HasExactRole(name));
        return role is null ? null : $"authorization role policy '{role}' was not recovered exactly";
    }

    /// <summary>
    /// Determines whether analysis recovered the policy completely.
    /// </summary>
    /// <param name="name">The policy name.</param>
    /// <returns>Whether exact recovery evidence exists.</returns>
    bool HasExactPolicy(string name) => Application?.Policies.Any(policy => policy.Name == name && PolicyRecovery.IsExact(policy)) == true;

    /// <summary>
    /// Determines whether the role's policy was recovered completely.
    /// </summary>
    /// <param name="role">The role name.</param>
    /// <returns>Whether its policy is exact.</returns>
    bool HasExactRole(string role) => HasExactPolicy(role);
}
