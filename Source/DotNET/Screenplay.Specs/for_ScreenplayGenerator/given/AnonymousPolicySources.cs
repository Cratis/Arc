// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Policies;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

internal static class AnonymousPolicySources
{
    internal const string Unrepresentable = "SP0082";
    internal const string Ambiguous = "SP0083";
    internal const string Authorization = "[Authorize(Policy = \"Anyone\")]";
    internal const string Registration = "services.AddArcAuthorizationPolicy<AnyonePolicy>(\"Anyone\", evaluatesAnonymous: true);";
    internal const string Body = "=> ValueTask.FromResult(true);";
    internal const string Controllers = """
        namespace Microsoft.AspNetCore.Mvc
        {
            public abstract class ControllerBase;
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public sealed class HttpPostAttribute : System.Attribute;
            [System.AttributeUsage(System.AttributeTargets.Method)]
            public sealed class HttpGetAttribute : System.Attribute;
            [System.AttributeUsage(System.AttributeTargets.Parameter)]
            public sealed class FromBodyAttribute : System.Attribute;
        }

        namespace Library.Catalog.Browsing
        {
            public record RegisterInterest(string InterestId);
            [Cratis.Arc.Queries.ModelBound.ReadModel]
            public record Interest(string InterestId);

            [Cratis.Arc.Authorization.Authorize(Policy = "Anyone")]
            public class InterestsController : Microsoft.AspNetCore.Mvc.ControllerBase
            {
                [Microsoft.AspNetCore.Mvc.HttpPost]
                public void Register([Microsoft.AspNetCore.Mvc.FromBody] RegisterInterest command) { }

                [Microsoft.AspNetCore.Mvc.HttpGet]
                public System.Collections.Generic.IEnumerable<Interest> AllInterests() => [];
            }
        }
        """;

    internal static (string Path, string Text)[] All(
        string authorization = Authorization,
        string registration = Registration,
        string extraRegistrations = "",
        string readModelAuthorization = "") =>
        Sources(Policies(), Composition(registration, extraRegistrations), Operations(authorization, readModelAuthorization));

    internal static (string Path, string Text)[] WithBody(string body, string methodModifier = "") =>
        Sources(Policies(body, methodModifier), Composition(), Operations());

    internal static (string Path, string Text)[] WithInheritedAuthorization(string authorization) =>
        Sources(Policies(), Composition(), Operations(baseAuthorization: authorization));

    static (string Path, string Text)[] Sources(string policies, string composition, string operations) =>
    [
        ("Library/Policies.cs", policies),
        ("Library/Authorization.cs", for_ApplicationModelAnalyzer.when_analyzing.PolicySource.Framework),
        ("Library/Composition.cs", composition),
        (Analyzed.SlicePath, operations)
    ];

    internal static (string Path, string Text)[] Shared(string queryAuthorization = "[Authorize(Policy = \"Anyone\"), Authorize]") =>
        Sources(Policies(), Composition(), Operations(queryAuthorization: queryAuthorization));

    internal static (string Path, string Text)[] WithController(bool query, bool shared, string policy = "Anyone")
    {
        const string Command = """
                [Microsoft.AspNetCore.Mvc.HttpPost]
                public void Register([Microsoft.AspNetCore.Mvc.FromBody] RegisterInterest command) { }
        """;
        const string Query = """
                [Microsoft.AspNetCore.Mvc.HttpGet]
                public System.Collections.Generic.IEnumerable<Interest> AllInterests() => [];
        """;
        var controller = Controllers.Replace(query ? Command.Trim() : Query.Trim(), string.Empty, StringComparison.Ordinal)
            .Replace("Library.Catalog.Browsing", "Library.Catalog.ControllerBrowsing", StringComparison.Ordinal)
            .Replace("Policy = \"Anyone\"", $"Policy = \"{policy}\"", StringComparison.Ordinal);
        var sources = Sources(Policies(), Composition(), controller);
        return shared ? [.. sources, ("Library/Catalog/PublicBrowsing/Public.cs", Operations())] : sources;
    }

    internal static ScreenplayGenerationResult Generate(params (string Path, string Text)[] sources)
    {
        var compilation = Analyzed.Compile(sources);
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
        return new ScreenplayGenerator().Generate(compilation, new());
    }

    internal static void AssertRefused(ScreenplayGenerationResult result, string code = Unrepresentable)
    {
        AssertRefusalDiagnostics(result.Diagnostics, code);
        result.Source.ShouldBeEmpty();
        result.Verification.ShouldBeNull();
        result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotCompile).ShouldBeEmpty();
    }

    internal static void AssertRefused(ScreenplayEmission emission, string code = Unrepresentable)
    {
        AssertRefusalDiagnostics(emission.Diagnostics, code);
        emission.Source.ShouldBeEmpty();
        emission.Application.Modules.ShouldBeEmpty();
        emission.Application.Policies.ShouldBeEmpty();
    }

    static void AssertRefusalDiagnostics(IReadOnlyList<ScreenplayDiagnostic> diagnostics, string code)
    {
        diagnostics.Where(diagnostic => diagnostic.Code == code && diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldNotBeEmpty();
        var refusals = diagnostics.Where(diagnostic => string.Equals(diagnostic.Code, Unrepresentable, StringComparison.Ordinal) || string.Equals(diagnostic.Code, Ambiguous, StringComparison.Ordinal)).ToArray();
        refusals.Select(diagnostic => (diagnostic.Code, diagnostic.Location, diagnostic.Message)).Distinct().Count().ShouldEqual(refusals.Length);
    }

    internal static SemanticSpecificationRun Run(ApplicationModel model, bool query, bool authenticated, bool denied)
    {
        var authorization = query
            ? model.Slices.SelectMany(slice => slice.Queries).Single().Authorization!
            : model.Slices.SelectMany(slice => slice.Commands).Single().Authorization!;
        var authorize = new AuthorizeSyntaxBuilder();
        var gate = authorize.Build(authorization)!;
        var referenced = authorize.Referenced.ToHashSet(StringComparer.Ordinal);
        var policies = new PolicySyntaxBuilder(new ScreenplayNaming()).Build(model.Policies, referenced).ToList();
        var source = new ScreenplayPrinter().Print(Document(gate, policies, authenticated, denied));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("AnonymousPolicyProof"));
        const string Key = "authorization";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "authorization.play", source);
        var bound = new SemanticModelCompiler().Compile("AnonymousPolicyProof", SemanticDocumentSet.Create([document], catalog));
        Assert.True(bound.Success, string.Join(Environment.NewLine, bound.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        var plan = SemanticExecutionPlan.Compile(bound.Value!.Model).Plan!;
        return new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
    }

    static ApplicationSyntax Document(AuthorizeSyntax gate, IReadOnlyList<PolicySyntax> policies, bool authenticated, bool denied)
    {
        var command = new CommandSyntax("Invoke", [], gate, [], [], null, SourceLocation.Start);
        var scenario = new SpecificationSyntax("Caller", [], new("Invoke", [], SourceLocation.Start), [], [], SourceLocation.Start)
        {
            GivenCaller = new(authenticated, [], [], SourceLocation.Start),
            ThenDenied = denied ? new(SourceLocation.Start) : null,
            ThenNoEvents = !denied
        };
        var slice = new SliceSyntax(SliceType.StateChange, "Gate", [], [command], [], [], [], [], [], [], [scenario], SourceLocation.Start);
        var feature = new FeatureSyntax("Authorization", [], [slice], SourceLocation.Start);
        var module = new ModuleSyntax("Proof", [], [feature], SourceLocation.Start);
        return new([], [], policies, [module], SourceLocation.Start);
    }

    internal static string Policies(string body = Body, string methodModifier = "") => $$"""
        using System.Threading;
        using System.Threading.Tasks;
        using Cratis.Arc.Authorization;

        namespace Library;

        public sealed class AnyonePolicy : IAuthorizationPolicy
        {
            public {{methodModifier}} ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
                {{body}}

            static bool Accept() => true;
        }

        public sealed class ProtectedPolicy : IAuthorizationPolicy
        {
            public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
                => ValueTask.FromResult(context.Principal.Identity?.IsAuthenticated == true);
        }

        public sealed class RolePolicy : IAuthorizationPolicy
        {
            public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)
                => ValueTask.FromResult(context.Principal.IsInRole("Staff"));
        }
        """;

    internal static string Composition(string registration = Registration, string extraRegistrations = "") => $$"""
        using Cratis.Arc.Authorization;
        using Microsoft.Extensions.DependencyInjection;

        namespace Library;

        public static class Composition
        {
            public static void Configure(IServiceCollection services, bool anonymous = true)
            {
                {{registration}}
                services.AddArcAuthorizationPolicy<ProtectedPolicy>("Protected");
                services.AddArcAuthorizationPolicy<RolePolicy>("StaffOnly", evaluatesAnonymous: true);
                {{extraRegistrations}}
            }
        }
        """;

    internal static string Operations(string authorization = Authorization, string readModelAuthorization = "", string baseAuthorization = "", string? queryAuthorization = null) => $$"""
        using System.Collections.Generic;
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library.Catalog.Browsing;

        {{baseAuthorization}}
        public record OperationBase;

        [Command] {{authorization}}
        public record RegisterInterest(string Id) : OperationBase
        {
            public void Handle() { }
        }

        [ReadModel] {{readModelAuthorization}}
        public record Interest(string InterestId) : OperationBase
        {
            {{queryAuthorization ?? authorization}}
            public static IEnumerable<Interest> AllInterests() => [];
        }
        """;
}
