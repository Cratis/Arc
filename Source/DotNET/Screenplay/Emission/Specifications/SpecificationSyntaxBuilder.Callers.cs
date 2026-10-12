// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Specifications;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

public partial class SpecificationSyntaxBuilder
{
    static bool RequiresAuthorization(AuthorizationModel? authorization) =>
        authorization is not null && HasAuthorizationRequirement(authorization);

    static bool HasAuthorizationRequirement(AuthorizationModel authorization) =>
        authorization.RequiresAuthentication || HasDeclaredPolicies(authorization);

    static bool HasDeclaredPolicies(AuthorizationModel authorization) => authorization.Roles.Any() || authorization.Policies.Any();

    static SpecificationCallerSyntax? Caller(SpecificationCallerModel? caller) => caller is null ? null :
        new(caller.Authenticated, caller.Roles, caller.Claims.Select(claim => new SpecificationCallerClaimSyntax(claim.Key, claim.Value, SourceLocation.Start)), SourceLocation.Start);

    SpecificationModel? WithRepresentableCaller(SpecificationModel specification)
    {
        var command = specification.When is { Kind: SpecificationStateKind.Command } issued
            ? Application?.Slices.FirstOrDefault(slice => slice.Specifications.Contains(specification))?.Commands.FirstOrDefault(command => command.Name == issued.Name)
            : null;
        var authorized = RequiresAuthorization(command?.Authorization);
        var reason = specification.CallerFailure;
        if (authorized)
        {
            reason ??= specification.Caller is null ? "no caller was recovered for its authorized command" : null;
            reason ??= InexactPolicy(command!.Authorization!);
        }

        if (reason is null)
        {
            return specification;
        }

        var location = SpecificationEvidence.For(specification)?.SourceType.ToDisplayString();
        if (authorized)
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
        return specification with { Caller = null };
    }

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

    bool HasExactPolicy(string name) => Application?.Policies.Any(policy => policy.Name == name && PolicyRecovery.IsExact(policy)) == true;

    bool HasExactRole(string role) => HasExactPolicy(role);
}
