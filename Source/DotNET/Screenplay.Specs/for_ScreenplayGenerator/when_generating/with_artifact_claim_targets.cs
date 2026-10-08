// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay;
using Cratis.Screenplay.Printing;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

/// <summary>
/// Guarded claim comparisons preserve their targets and unreadable assertions remain diagnosed.
/// </summary>
public class with_artifact_claim_targets : Specification
{
    /// <summary>
    /// The policy-registration contracts used by these source fixtures.
    /// </summary>
    public const string Framework = """
        using System;
        using System.Security.Claims;
        namespace Microsoft.AspNetCore.Authorization;
        public class AuthorizationHandlerContext
        {
            public object Resource { get; init; } = null!;
            public ClaimsPrincipal User { get; init; } = null!;
        }
        public class AuthorizationPolicyBuilder
        {
            public AuthorizationPolicyBuilder RequireAssertion(Func<AuthorizationHandlerContext, bool> handler) => this;
        }
        public class AuthorizationOptions
        {
            public void AddPolicy(string name, Action<AuthorizationPolicyBuilder> configure) { }
        }
        """;

    const string Command = """
        using Cratis.Arc.Authorization;
        using Cratis.Arc.Commands.ModelBound;
        using Cratis.Chronicle.Keys;
        namespace Library.Departments.Updating;
        public record Address(string Region);
        [Command, Authorize(Policy = "CanUpdate")]
        public record UpdateDepartment([property: Key] string Id, string Department, Address Address)
        {
            public void Handle() { }
        }
        """;

    static (string Path, string Text)[] Sources(string condition, string command = Command) =>
    [
        ("Library/Authorization.cs", Framework),
        ("Library/Departments/Updating/UpdateDepartment.cs", command),
        ("Library/Composition.cs", $$"""
            using Microsoft.AspNetCore.Authorization;
            using Cratis.Arc.Commands;
            using Library.Departments.Updating;
            namespace Library;
            public static class Composition
            {
                public static void Configure(AuthorizationOptions options) =>
                    options.AddPolicy("CanUpdate", policy => policy.RequireAssertion(context =>
                        context.Resource is CommandContext { Command: UpdateDepartment command } && {{condition}}));
            }
            """)
    ];

    static ScreenplayGenerationResult Generate(string condition, string command = Command)
    {
        var sources = Sources(condition, command);
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();

        return new ScreenplayGenerator().Generate(Analyzed.Compile(sources), new());
    }

    static void ShouldBind(ScreenplayGenerationResult result)
    {
        var compiled = new ScreenplayCompiler().Compile(result.Source);
        compiled.Diagnostics.ShouldBeEmpty();
        new ScreenplayVerifier().Verify(result.Source).BindingDiagnostics.Where(d => d.Severity != Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Information).ShouldBeEmpty();
        result.Diagnostics.ShouldBeEmpty();
        new ScreenplayPrinter().Print(compiled.Value!).ShouldEqual(result.Source);
    }

    [Fact]
    void should_match_the_subject()
    {
        var result = Generate("context.User.HasClaim(\"owner\", command.Id)");
        result.Source.ShouldContain("require claim \"owner\" matches subject");
        ShouldBind(result);
    }

    [Fact]
    void should_match_a_property_expression()
    {
        var result = Generate("context.User.HasClaim(\"department\", command.Department)");
        result.Source.ShouldContain("require claim \"department\" matches department");
        ShouldBind(result);
    }

    [Fact]
    void should_match_a_nested_property_expression()
    {
        var result = Generate("context.User.HasClaim(\"region\", command.Address.Region)");
        result.Source.ShouldContain("require claim \"region\" matches address.region");
        ShouldBind(result);
    }

    [Fact]
    void should_match_value_without_inventing_a_subject()
    {
        const string WithoutIdentifier = """
            using Cratis.Arc.Authorization;
            using Cratis.Arc.Commands.ModelBound;
            namespace Library.Departments.Updating;
            [Command, Authorize(Policy = "CanUpdate")]
            public record UpdateDepartment(string Value)
            {
                public void Handle() { }
            }
            """;
        var result = Generate("context.User.HasClaim(\"owner\", command.Value)", WithoutIdentifier);
        result.Source.ShouldContain("require claim \"owner\" matches value");
        result.Source.ShouldNotContain("matches subject");
        ShouldBind(result);
    }

    [Theory]
    [InlineData("type: \"department\", value: command.Department")]
    [InlineData("value: command.Department, type: \"department\"")]
    [InlineData("\"department\", value: command.Department")]
    void should_bind_claim_arguments_to_their_parameters(string arguments)
    {
        var result = Generate($"context.User.HasClaim({arguments})");
        result.Source.ShouldContain("require claim \"department\" matches department");
        ShouldBind(result);
    }

    [Fact]
    void should_not_swap_a_dynamic_claim_type_with_a_constant_value()
    {
        var result = Generate("context.User.HasClaim(value: \"department\", type: command.Department)");
        result.Source.ShouldContain("require authenticated");
        result.Source.ShouldNotContain("matches");
        result.Diagnostics.Count(d => d.Code == "SP0026").ShouldEqual(1);
    }

    [Theory]
    [InlineData("public record Address(string Region);")]
    [InlineData("public record Address(string Input) { public string Region => Input.ToUpperInvariant(); }")]
    void should_not_state_a_property_path_through_a_compiled_record(string declaration)
    {
        var package = Analyzed.Package("Addresses", $"namespace Addresses; {declaration}");
        var sources = Sources("context.User.HasClaim(\"region\", command.Address.Region)",
            Command.Replace("public record Address(string Region);", "", StringComparison.Ordinal).Replace("Address Address", "Addresses.Address Address", StringComparison.Ordinal));
        var compilation = Analyzed.Compile([package], sources);
        Analyzed.ErrorsIn(compilation).ShouldBeEmpty();
        var result = new ScreenplayGenerator().Generate(compilation, new());
        result.Source.ShouldContain("require authenticated");
        result.Source.ShouldNotContain("matches");
        result.Diagnostics.Count(d => d.Code == "SP0026").ShouldEqual(1);
        result.Diagnostics.Where(d => d.Code == "SP0056").ShouldBeEmpty();
    }

    [Fact]
    void should_not_state_the_metadata_value_of_a_concept_as_a_property_path()
    {
        var command = Command.Replace("public record Address(string Region);", "public record Department(string Value) : Cratis.Concepts.ConceptAs<string>(Value);", StringComparison.Ordinal)
            .Replace("string Department, Address Address", "Department Department", StringComparison.Ordinal);
        var result = Generate("context.User.HasClaim(\"department\", command.Department.Value)", command);
        result.Source.ShouldContain("require authenticated");
        result.Source.ShouldNotContain("matches");
        result.Diagnostics.Count(d => d.Code == "SP0026").ShouldEqual(1);
        result.Diagnostics.Where(d => d.Code == "SP0056").ShouldBeEmpty();
    }

    [Fact]
    void should_preserve_logical_grouping()
    {
        var result = Generate("context.User.HasClaim(\"department\", command.Department) && (context.User.HasClaim(\"owner\", command.Id) || context.User.HasClaim(\"region\", command.Address.Region))");
        result.Source.ShouldContain("require claim \"department\" matches department and (claim \"owner\" matches subject or claim \"region\" matches address.region)");
        ShouldBind(result);
    }

    [Theory]
    [InlineData("!context.User.HasClaim(\"owner\", command.Id)")]
    [InlineData("context.User.HasClaim(\"department\", command.Department.ToLowerInvariant())")]
    [InlineData("context.User.FindFirst(\"owner\")!.Value == command.Id")]
    [InlineData("context.User.HasClaim(\"owner\", command.Id) || true")]
    [InlineData("context.User.HasClaim(\"owner\", command.Id) && DateTime.Now.Day == 1")]
    void should_report_an_unreadable_assertion_without_stating_a_partial_condition(string condition)
    {
        var result = Generate(condition.Replace("DateTime", "System.DateTime", StringComparison.Ordinal));
        result.Source.ShouldContain("require authenticated");
        result.Source.ShouldNotContain("matches");
        result.Diagnostics.Count(d => d.Code == "SP0026").ShouldEqual(1);
        result.Diagnostics.Where(d => d.Code == "SP0056").ShouldBeEmpty();
    }

    [Fact]
    void should_read_a_parenthesized_block_lambda()
    {
        var sources = Sources("context.User.HasClaim(\"owner\", command.Id)");
        sources[^1].Text = sources[^1].Text.Replace("context =>", "(context) => { return", StringComparison.Ordinal)
            .Replace("command.Id)));", "command.Id); }));", StringComparison.Ordinal);
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        var result = new ScreenplayGenerator().Generate(Analyzed.Compile(sources), new());
        result.Source.ShouldContain("require claim \"owner\" matches subject");
        ShouldBind(result);
    }

    [Fact]
    void should_not_state_a_conditionally_registered_assertion_as_unconditional()
    {
        var sources = Sources("context.User.HasClaim(\"owner\", command.Id)");
        sources[^1].Text = sources[^1].Text.Replace("policy => policy.RequireAssertion(", "policy => { if (System.DateTime.UtcNow.Day == 1) policy.RequireAssertion(", StringComparison.Ordinal)
            .Replace("command.Id)));", "command.Id)); });", StringComparison.Ordinal);
        Analyzed.ErrorsIn(sources).ShouldBeEmpty();
        var result = new ScreenplayGenerator().Generate(Analyzed.Compile(sources), new());
        result.Diagnostics.Count(d => d.Code == "SP0026").ShouldEqual(1);
        result.Source.ShouldContain("require authenticated");
    }

    [Fact]
    void should_not_state_a_computed_getter_as_an_artifact_property()
    {
        var command = Command.Replace("public void Handle() { }", "public string Computed => Department.ToUpperInvariant(); public void Handle() { }", StringComparison.Ordinal);
        var result = Generate("context.User.HasClaim(\"department\", command.Computed)", command);
        result.Diagnostics.Count(d => d.Code == "SP0026").ShouldEqual(1);
        result.Source.ShouldContain("require authenticated");
    }

    [Fact]
    void should_not_state_a_nullable_target_that_may_throw_in_has_claim()
    {
        var result = Generate("context.User.HasClaim(\"department\", command.Department!)", Command.Replace("string Department", "string? Department", StringComparison.Ordinal));
        result.Diagnostics.Count(d => d.Code == "SP0026").ShouldEqual(1);
        result.Source.ShouldContain("require authenticated");
    }

    [Fact]
    void should_not_discard_a_resource_guard_for_a_policy_used_on_another_command()
    {
        const string Other = "[Command, Authorize(Policy = \"CanUpdate\")] public record Other(string Department) { public void Handle() { } }";
        var result = Generate("context.User.HasClaim(\"department\", command.Department)", Command + Other);
        result.Diagnostics.Count(d => d.Code == "SP0026").ShouldEqual(1);
        result.Source.ShouldContain("require authenticated");
        result.Diagnostics.Where(d => d.Code == "SP0056").ShouldBeEmpty();
    }
}
