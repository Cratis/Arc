// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Embedded.Generation;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Screenplay.Embedded.for_DocumentGeneration.when_generating_documents;

public class from_refused_anonymous_policy_use : Specification
{
    const string Unrepresentable = "SP0082";
    const string Ambiguous = "SP0083";

    [Fact]
    public void should_keep_synthetic_refusals_empty_without_verifying_or_manufacturing_placeholder_documents()
    {
        var verifier = new recording_verifier();
        var generation = new EmbeddedDocumentGenerator(new ApplicationModelAnalyzer(), new refusing_emitter(), verifier).Generate(
            given.an_application.Build(), new("Library", "Library"), []);
        AssertRefused(generation, verifier, Unrepresentable);
        var refusals = generation.Diagnostics.Where(diagnostic => diagnostic.Code == Unrepresentable).ToArray();
        refusals.Length.ShouldEqual(2);
        refusals.Select(diagnostic => diagnostic.Location).ShouldContainOnly(["Library.Catalog.First", "Library.Catalog.Second"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_preserve_real_shared_use_and_registration_collision_refusals_across_all_embedded_scopes(bool ambiguous)
    {
        var compilation = Compile(protectedQuery: true, ambiguous: ambiguous);
        var analysis = new ApplicationModelAnalyzer().Analyze(compilation, new());
        analysis.Model.Policies.Single(policy => policy.Name == "Anyone").Requirement.ShouldBeOfExactType<AnonymousRequirement>();
        if (!ambiguous)
        {
            analysis.Model.Slices.SelectMany(slice => slice.Commands).Single().Authorization!.RequiresAuthentication.ShouldBeFalse();
            analysis.Model.Slices.SelectMany(slice => slice.Queries).Single().Authorization!.RequiresAuthentication.ShouldBeTrue();
        }

        var verifier = new recording_verifier();
        var generator = new EmbeddedDocumentGenerator(new ApplicationModelAnalyzer(), new ScreenplayEmitter(), verifier);
        var generation = generator.Generate(analysis.Model, new("Library", "Library"), analysis.Diagnostics);
        AssertRefused(generation, verifier, ambiguous ? Ambiguous : Unrepresentable);
        analysis.Model.Policies.Single(policy => policy.Name == "Anyone").Requirement.ShouldBeOfExactType<AnonymousRequirement>();
    }

    [Fact]
    public void should_still_verify_supported_public_only_documents_once_bound_and_each_scoped_syntax()
    {
        var verifier = new recording_verifier();
        var generation = new EmbeddedDocumentGenerator(new ApplicationModelAnalyzer(), new ScreenplayEmitter(), verifier).Generate(
            Compile(protectedQuery: false, ambiguous: false), new("Library", "Library"));
        generation.IsSuccess.ShouldBeTrue();
        generation.Documents.ShouldNotBeEmpty();
        generation.Documents.All(document => document.Source.Length > 0).ShouldBeTrue();
        verifier.BoundSources.Count.ShouldEqual(1);
        verifier.SyntaxSources.ShouldNotBeEmpty();
    }

    static void AssertRefused(EmbeddedDocumentGeneration generation, recording_verifier verifier, string code)
    {
        generation.IsSuccess.ShouldBeFalse();
        generation.Diagnostics.Where(diagnostic => diagnostic.Code == code && diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldNotBeEmpty();
        generation.Documents.Where(document => document.Source.Length > 0).ShouldBeEmpty();
        generation.Resources.Where(resource => resource.Key != EmbeddedResourceNames.Catalog && resource.Value.Length > 0).ShouldBeEmpty();
        verifier.BoundSources.ShouldBeEmpty();
        verifier.SyntaxSources.ShouldBeEmpty();
        var refusals = generation.Diagnostics.Where(diagnostic => string.Equals(diagnostic.Code, Unrepresentable, StringComparison.Ordinal) || string.Equals(diagnostic.Code, Ambiguous, StringComparison.Ordinal)).ToArray();
        refusals.Select(diagnostic => (diagnostic.Code, diagnostic.Location, diagnostic.Message)).Distinct().Count().ShouldEqual(refusals.Length);
        generation.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotCompile).ShouldBeEmpty();
    }

    static CSharpCompilation Compile(bool protectedQuery, bool ambiguous)
    {
        var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "Library",
            [CSharpSyntaxTree.ParseText(Source(protectedQuery, ambiguous), path: "Library/Operations.cs"), CSharpSyntaxTree.ParseText("namespace Library;", path: "Library/Program.cs")],
            references,
            new(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToArray();
        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        return compilation;
    }

    static string Source(bool protectedQuery, bool ambiguous) => string.Join(
        '\n',
        [
            "using System.Collections.Generic;",
            "using System.Threading;",
            "using System.Threading.Tasks;",
            "using Cratis.Arc.Authorization;",
            "using Cratis.Arc.Commands.ModelBound;",
            "using Cratis.Arc.Queries.ModelBound;",
            "using Microsoft.Extensions.DependencyInjection;",
            "",
            "namespace Library",
            "{",
            "    public sealed class AnyonePolicy : IAuthorizationPolicy",
            "    {",
            "        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)",
            "            => ValueTask.FromResult(true);",
            "    }",
            "",
            "    public sealed class ProtectedPolicy : IAuthorizationPolicy",
            "    {",
            "        public ValueTask<bool> IsAuthorized(AuthorizationPolicyContext context, CancellationToken cancellationToken)",
            "            => ValueTask.FromResult(context.Principal.Identity?.IsAuthenticated == true);",
            "    }",
            "",
            "    public static class Composition",
            "    {",
            "        public static void Configure(IServiceCollection services)",
            "        {",
            "            services.AddArcAuthorizationPolicy<AnyonePolicy>(\"Anyone\", evaluatesAnonymous: true);",
            $"            {(ambiguous ? "services.AddArcAuthorizationPolicy<ProtectedPolicy>(\"anyone\");" : string.Empty)}",
            "        }",
            "    }",
            "}",
            "",
            "namespace Library.Catalog.PublicBrowsing",
            "{",
            "    [Command, Authorize(Policy = \"Anyone\")]",
            "    public record RegisterInterest(string InterestId)",
            "    {",
            "        public void Handle() { }",
            "    }",
            "}",
            "",
            "namespace Library.Catalog.ProtectedBrowsing",
            "{",
            "    [ReadModel]",
            "    public record Interest(string InterestId)",
            "    {",
            "        [Authorize(Policy = \"Anyone\")]",
            $"        {(protectedQuery ? "[Authorize]" : string.Empty)}",
            "        public static IEnumerable<Interest> AllInterests() => [];",
            "    }",
            "}"
        ]);

    sealed class refusing_emitter : IScreenplayEmitter
    {
        public ScreenplayEmission Emit(ApplicationModel model, ScreenplayOptions options)
        {
            var first = new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, Unrepresentable, "Anyone cannot represent the protected first operation.", "Library.Catalog.First");
            var second = new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, Unrepresentable, "Anyone cannot represent the protected second operation.", "Library.Catalog.Second");
            return new(string.Empty, new([], [], [], [], SourceLocation.Start), [first, first, second]);
        }
    }

    sealed class recording_verifier : IScreenplayVerifier
    {
        readonly ScreenplayVerifier _inner = new();
        readonly ConcurrentQueue<string> _boundSources = new();
        readonly ConcurrentQueue<string> _syntaxSources = new();

        public IReadOnlyList<string> BoundSources => [.. _boundSources];

        public IReadOnlyList<string> SyntaxSources => [.. _syntaxSources];

        public ScreenplayVerification Verify(string source)
        {
            _boundSources.Enqueue(source);
            return _inner.Verify(source);
        }

        public ScreenplayVerification VerifySyntax(string source)
        {
            _syntaxSources.Enqueue(source);
            return _inner.VerifySyntax(source);
        }
    }
}
