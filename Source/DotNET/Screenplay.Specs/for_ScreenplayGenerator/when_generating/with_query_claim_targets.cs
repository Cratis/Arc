// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Guarded query-key comparisons preserve their subject without discarding dictionary lookup guards.
/// </summary>
public class with_query_claim_targets : Specification
{
    const string Query = """
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Queries.ModelBound;
        namespace Library.Authors.Listing;
        [ReadModel]
        public record Author(string Id, string Name)
        {
            [Authorize(Policy = "CanRead")]
            public static Author? ById(string id) => null;
        }
        """;

    const string Condition = """
        context.Resource is QueryContext { Arguments: { } arguments } &&
        arguments.TryGetValue("id", out var value) && value is string id &&
        context.User.HasClaim("owner", id)
        """;

    static ScreenplayGenerationResult Generate(string condition = Condition, string query = Query)
    {
        var sources = new (string Path, string Text)[]
        {
            ("Library/Authorization.cs", with_artifact_claim_targets.Framework),
            (Analyzed.SlicePath, query),
            ("Library/Composition.cs", $$"""
                using Microsoft.AspNetCore.Authorization;
                using Cratis.Arc.Queries;
                namespace Library;
                public static class Composition
                {
                    public static void Configure(AuthorizationOptions options) => options.AddPolicy("CanRead",
                        policy => policy.RequireAssertion(context => {{condition}}));
                }
                """)
        };
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();

        return new ScreenplayGenerator().Generate(Analyzed.Compile(sources), new());
    }

    [Fact]
    void should_recover_a_claim_matching_the_proven_query_key()
    {
        var result = Generate();
        result.Source.ShouldContain("require claim \"owner\" matches subject");
        result.Diagnostics.Where(d => d.Code != "SP0019").ShouldBeEmpty();
        new ScreenplayCompiler().Compile(result.Source).Diagnostics.ShouldBeEmpty();
        new ScreenplayVerifier().Verify(result.Source).BindingDiagnostics.Where(d => d.Severity != Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Information).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("arguments.TryGetValue(\"missing\", out var value) && value is string id")]
    [InlineData("arguments.TryGetValue(\"id\", out var value) && value is string id && id.Length > 0")]
    void should_not_invent_a_query_key_or_discard_additional_guards(string lookup)
    {
        var condition = Condition.Replace("arguments.TryGetValue(\"id\", out var value) && value is string id", lookup, StringComparison.Ordinal);
        var result = Generate(condition);
        result.Diagnostics.Count(d => d.Code == "SP0026").ShouldEqual(1);
        result.Source.ShouldContain("require authenticated");
        result.Diagnostics.Where(d => d.Code == "SP0056").ShouldBeEmpty();
    }

    [Fact]
    void should_not_recover_an_unguarded_dictionary_access()
    {
        var result = Generate("context.Resource is QueryContext { Arguments: { } arguments } && context.User.HasClaim(\"owner\", (string)arguments[\"id\"])");
        result.Diagnostics.Count(d => d.Code == "SP0026").ShouldEqual(1);
        result.Source.ShouldContain("require authenticated");
    }

    [Fact]
    void should_not_discard_guards_when_the_policy_is_used_by_a_non_keyed_query()
    {
        var result = Generate(query: Query.Replace("ById(string id)", "ById(string id, string name)", StringComparison.Ordinal));
        result.Diagnostics.Count(d => d.Code == "SP0026").ShouldEqual(1);
        result.Source.ShouldContain("require authenticated");
        result.Diagnostics.Where(d => d.Code == "SP0056").ShouldBeEmpty();
    }
}
