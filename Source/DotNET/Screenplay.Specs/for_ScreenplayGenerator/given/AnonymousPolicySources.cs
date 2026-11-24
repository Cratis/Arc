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
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

internal static class AnonymousPolicySources
{
    internal const string Unrepresentable = "SP0082";
    internal const string Ambiguous = "SP0083";
    internal const string Authorization = "[Authorize(Policy = \"Anyone\")]";
    internal const string Registration = "services.AddArcAuthorizationPolicy<AnyonePolicy>(\"Anyone\", evaluatesAnonymous: true);";
    internal const string Body = "=> ValueTask.FromResult(true);";
    internal const string MicrosoftAdmin = "[Microsoft.AspNetCore.Authorization.Authorize(Roles = \"Admin\")]";
    internal const string MicrosoftBare = "[Microsoft.AspNetCore.Authorization.Authorize]";
    internal const string MicrosoftPolicy = "[Microsoft.AspNetCore.Authorization.Authorize(\"AdminPolicy\")]";
    internal const string MicrosoftNamedPolicy = "[Microsoft.AspNetCore.Authorization.Authorize(Policy = \"AdminPolicy\")]";
    internal const string MicrosoftSchemes = "[Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = \"Bearer\")]";
    internal const string DerivedAdmin = "[Library.AdminGate]";
    internal const string IndirectAdmin = "[Library.IndirectAdminGate]";

    /// <summary>
    /// Scope ceilings narrow verified callers; their recovery refusal does not imply universal caller denial.
    /// </summary>
    internal const string RequiredScope = "[Cratis.Arc.Authorization.RequiresScope(\"catalog:read\")]";
    internal const string BareProtectedOperations = """

        [Command, Authorize]
        public record ExecuteProtected(string Id)
        {
            public void Handle() { }
        }

        [ReadModel]
        public record ProtectedInterest(string ProtectedInterestId)
        {
            [Authorize]
            public static IEnumerable<ProtectedInterest> ListProtected() => [];
        }
        """;
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

    internal static TheoryData<string, string> AdditionalGateCases()
    {
        var cases = new TheoryData<string, string>();
        foreach (var gate in new[] { MicrosoftAdmin, MicrosoftBare, MicrosoftPolicy, MicrosoftNamedPolicy, MicrosoftSchemes, DerivedAdmin, IndirectAdmin, RequiredScope })
        {
            foreach (var placement in new[] { "command", "query", "query-type", "command-base", "query-base" })
            {
                cases.Add(gate, placement);
            }
        }

        return cases;
    }

    internal static Compilation SecurityCompilation(string gate, string placement)
    {
        var sources = Sources(Policies(), Composition(), SecurityOperations(gate, placement));
        var compilation = Analyzed.Compile(sources);
        if (compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Authorization.AuthorizeAttribute") is null)
        {
            var runtime = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            var shared = Directory.GetParent(runtime)!.Parent!.FullName;
            var assembly = Path.Combine(shared, "Microsoft.AspNetCore.App", Path.GetFileName(runtime), "Microsoft.AspNetCore.Authorization.dll");
            Assert.True(File.Exists(assembly), $"Real Microsoft authorization metadata is required: {assembly}");
            compilation = Analyzed.Compile([MetadataReference.CreateFromFile(assembly)], sources);
        }

        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
        compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Authorization.AuthorizeAttribute")!.ContainingAssembly.Name.ShouldEqual("Microsoft.AspNetCore.Authorization");
        return compilation;
    }

    internal static (string Path, string Text)[] ReservedAuthentication(string name, bool anonymous = true)
    {
        var type = anonymous ? "AnyonePolicy" : "ProtectedPolicy";
        var registration = $"services.AddArcAuthorizationPolicy<{type}>(\"{name}\", evaluatesAnonymous: true);";
        return Sources(Policies(), Composition(registration), Operations($"[Authorize(Policy = \"{name}\")]") + Environment.NewLine + BareProtectedOperations);
    }

    internal static Compilation OverrideCompilation(string gate, bool declaringType)
    {
        var compilation = SecurityCompilation(gate, "query").AddSyntaxTrees(
            Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(
                $$"""
                namespace Library.Catalog.Browsing;

                {{(declaringType ? gate : string.Empty)}}
                public class BaseQueryAncestor
                {
                    {{(declaringType ? string.Empty : gate)}}
                    public virtual System.Collections.Generic.IEnumerable<Interest> AllInterests() => [];
                }

                public class BaseQueries : BaseQueryAncestor;

                public class OverrideQueries : BaseQueries
                {
                    [Cratis.Arc.Authorization.Authorize(Policy = "Anyone")]
                    public override System.Collections.Generic.IEnumerable<Interest> AllInterests() => [];
                }
                """,
                path: "Library/Catalog/Browsing/Overrides.cs"));
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
        return compilation;
    }

    static string SecurityOperations(string gate, string placement) => $$"""
        using System.Collections.Generic;
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Arc.Queries.ModelBound;

        namespace Library
        {
            public class AdminGateAttribute : Microsoft.AspNetCore.Authorization.AuthorizeAttribute
            {
                public AdminGateAttribute() { Roles = "Admin"; }
            }

            public sealed class IndirectAdminGateAttribute : AdminGateAttribute;
        }

        namespace Library.Catalog.Browsing
        {
            {{(placement == "command-base" ? gate : string.Empty)}}
            public record CommandGateAncestor;
            public record CommandGateBase : CommandGateAncestor;

            {{(placement == "query-base" ? gate : string.Empty)}}
            public record ReadGateAncestor;
            public record ReadGateBase : ReadGateAncestor;

            [Command, Authorize(Policy = "Anyone")]
            {{(placement == "command" ? gate : string.Empty)}}
            public record RegisterInterest(string Id) : CommandGateBase
            {
                public void Handle() { }
            }

            [ReadModel]
            {{(placement == "query-type" ? gate : string.Empty)}}
            public record Interest(string InterestId) : ReadGateBase
            {
                [Authorize(Policy = "Anyone")]
                {{(placement == "query" ? gate : string.Empty)}}
                public static IEnumerable<Interest> AllInterests() => [];
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
