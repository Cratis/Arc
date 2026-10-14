// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Policies;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.Emission.Specifications;

/// <summary>
/// Proves denial using the pinned reference runner, with the emitted gate and caller and no command effects.
/// </summary>
/// <param name="naming">The naming used to emit the authorization policies.</param>
internal sealed class SpecificationDenialProof(IScreenplayNaming naming)
{
    const string Undecidable = "denial cannot be decided from the stated caller";

    /// <summary>
    /// Checks that authorization alone denies the stated caller.
    /// </summary>
    /// <param name="authorization">The emitted command's authorization.</param>
    /// <param name="declared">The application's recovered policies.</param>
    /// <param name="caller">The exact emitted caller.</param>
    /// <returns>The failure reason, or null when the runner proves an authorization rejection.</returns>
    internal string? Failure(AuthorizationModel authorization, IEnumerable<PolicyModel> declared, SpecificationCallerSyntax caller)
    {
        var authorize = new AuthorizeSyntaxBuilder();
        var gate = authorize.Build(authorization)!;
        var referenced = authorize.Referenced.ToHashSet(StringComparer.Ordinal);
        var policies = new PolicySyntaxBuilder(naming).Build(declared.Where(policy => referenced.Contains(PolicyNames.For(policy.Name))), referenced).ToList();
        if (policies.Exists(policy => !OnlyCaller(policy.Condition)))
        {
            return $"{Undecidable}; a policy needs a subject, artifact value, or opaque predicate";
        }

        var source = new ScreenplayPrinter().Print(Document(gate, policies, caller));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("DenialProof"));
        const string Key = "authorization";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "authorization.play", source);
        var bound = new SemanticModelCompiler().Compile("DenialProof", SemanticDocumentSet.Create([document], catalog));
        if (!bound.Success || SemanticExecutionPlan.Compile(bound.Value!.Model).Plan is not { } plan)
        {
            return Undecidable;
        }

        var run = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
        return run.Execution switch
        {
            SemanticRejected { Category: SemanticRejectionCategory.Unauthorized } when run.Passed => null,
            SemanticAccepted => "stated caller satisfies the stated authorization",
            _ => Undecidable
        };
    }

    static bool OnlyCaller(PolicyConditionSyntax? condition) => condition switch
    {
        AuthenticatedConditionSyntax or RoleConditionSyntax => true,
        ClaimConditionSyntax { MatchesSubject: false, Matches: LiteralExpressionSyntax } => true,
        LogicalPolicyConditionSyntax logical => OnlyCaller(logical.Left) && OnlyCaller(logical.Right),
        _ => false
    };

    static ApplicationSyntax Document(AuthorizeSyntax gate, IReadOnlyList<PolicySyntax> policies, SpecificationCallerSyntax caller)
    {
        var command = new CommandSyntax("Invoke", [], gate, [], [], null, SourceLocation.Start);
        var scenario = new SpecificationSyntax("Denial", [], new("Invoke", [], SourceLocation.Start), [], [], SourceLocation.Start)
        {
            GivenCaller = caller,
            ThenDenied = new(SourceLocation.Start)
        };
        var slice = new SliceSyntax(SliceType.StateChange, "Gate", [], [command], [], [], [], [], [], [], [scenario], SourceLocation.Start);
        var feature = new FeatureSyntax("Authorization", [], [slice], SourceLocation.Start);
        var module = new ModuleSyntax("Proof", [], [feature], SourceLocation.Start);

        return new([], [], policies, [module], SourceLocation.Start);
    }
}
