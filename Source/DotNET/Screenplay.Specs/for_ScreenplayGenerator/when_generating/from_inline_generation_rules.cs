// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_inline_generation_rules : a_generation_strategy
{
    [Theory]
    [InlineData("public static AuthorId New(GenerationContext context) => new(context.Now.ToString(\"yyyyMMdd\"));", "return context.Now.ToString(\"yyyyMMdd\");")]
    [InlineData("public static AuthorId New(GenerationContext context) { var id = Guid.NewGuid(); return new(id.ToString()); }", "var id = Guid.NewGuid(); return id.ToString();")]
    [InlineData("public static AuthorId New(GenerationContext context) { var now = context.Now; return new(now.ToString(\"yyyy\")); }", "var now = context.Now; return now.ToString(\"yyyy\");")]
    [InlineData("public static AuthorId New(GenerationContext context) => new($\"{context.Tenant}:{GenerateValue.Ulid(context)}\");", "return $\"{context.Tenant}:{GenerateValue.Ulid(context)}\";")]
    [InlineData("public static AuthorId New(GenerationContext context) => new($\"{context.Tenant}:{GenerateValue.UuidV7(context)}\");", "return $\"{context.Tenant}:{GenerateValue.UuidV7(context)}\";")]
    [InlineData("public static AuthorId New(GenerationContext context) => new($\"{context.Tenant}:{GenerateValue.TypeId(\"receipt\", context)}\");", "return $\"{context.Tenant}:{GenerateValue.TypeId(\"receipt\", context)}\";")]
    [InlineData("public static AuthorId New(GenerationContext context) { var tenant = context.Tenant; return new(tenant + \"-\" + GenerateValue.Ulid(context)); }", "var tenant = context.Tenant; return tenant + \"-\" + GenerateValue.Ulid(context);")]
    [InlineData("public static AuthorId New(GenerationContext context) => new(string.Concat(context.Tenant, \"-\", context.Now.ToString(\"yyyy\")));", "return string.Concat(context.Tenant, \"-\", context.Now.ToString(\"yyyy\"));")]
    public void should_copy_self_contained_bodies_with_only_forwarding_construction_removed(string factory, string body)
    {
        Generate((Analyzed.SlicePath, RuleSource(factory)));
        Assert.True(Result.Source.Contains("generate rule AuthorIdNew", StringComparison.Ordinal), string.Join('\n', Result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        AssertStrategy("generate rule AuthorIdNew");
        Result.Source.ShouldContain("```csharp");
        Result.Source.ShouldContain(body);
        Bound.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V21);
    }

    [Theory]
    [InlineData("public static AuthorId New(GenerationContext context) => new(context.Tenant == \"Default\" ? \"x\" : context.Tenant);")]
    [InlineData("public static AuthorId New(GenerationContext context) => new(DateTimeOffset.UtcNow.ToString());")]
    [InlineData("static string Prefix = \"abc\"; public static AuthorId New(GenerationContext context) => new(Prefix + context.Tenant);")]
    [InlineData("static string Helper() => \"abc\"; public static AuthorId New(GenerationContext context) => new(Helper() + context.Tenant);")]
    [InlineData("public static AuthorId New(GenerationContext context) => new(System.Environment.MachineName);")]
    public void should_refuse_ambient_state_and_external_dependencies(string factory)
    {
        Generate((Analyzed.SlicePath, RuleSource(factory)));
        Result.Source.ShouldNotContain("generate rule");
        Result.Source.ShouldNotContain("receipt AuthorId generated");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldBeTrue();
        AssertPartialDocument();
    }

    [Theory]
    [InlineData("public static AuthorId New(GenerationContext context) /* outside-opening */ { return new(context.Tenant); }")]
    [InlineData("public static AuthorId New(GenerationContext context) { return new(context.Tenant); } /* outside-closing */")]
    public void should_extract_statements_without_brace_trivia(string factory)
    {
        Generate((Analyzed.SlicePath, RuleSource(factory)));
        AssertStrategy("generate rule AuthorIdNew");
        var declaration = new ScreenplayCompiler().Compile(Result.Source).Value!.Concepts.Single(concept => concept.Name == "AuthorId");
        declaration.Generation!.Code!.Code.Trim().ShouldEqual("return context.Tenant;");
    }

    [Theory]
    [InlineData("using G = System.Guid;", "G.NewGuid().ToString()")]
    [InlineData("using static System.Math;", "string.Concat(Abs(-42))")]
    public void should_refuse_import_dependent_rule_bodies(string import, string expression)
    {
        var source = import + "\n" + RuleSource($"public static AuthorId New(GenerationContext context) => new({expression});");
        Generate((Analyzed.SlicePath, source));
        Result.Source.ShouldNotContain("generate rule");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldBeTrue();
        AssertPartialDocument();
    }

    [Theory]
    [InlineData("GenerateValue.Ulid()")]
    [InlineData("GenerateValue.UuidV7()")]
    [InlineData("GenerateValue.TypeId(\"receipt\")")]
    public void should_refuse_ambient_time_inside_compound_rules(string call)
    {
        Generate((Analyzed.SlicePath, RuleSource($"public static AuthorId New(GenerationContext context) => new($\"prefix:{{{call}}}\");")));
        Result.Source.ShouldNotContain("generate rule");
        Result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldBeTrue();
        AssertPartialDocument();
    }

    protected static string RuleSource(string factory) => Source("GenerateValue.Ulid()")
        .Replace("IGeneratable<AuthorId>", "IGeneratableInContext<AuthorId>", StringComparison.Ordinal)
        .Replace("public static AuthorId New() => new(GenerateValue.Ulid());", factory, StringComparison.Ordinal);
}
