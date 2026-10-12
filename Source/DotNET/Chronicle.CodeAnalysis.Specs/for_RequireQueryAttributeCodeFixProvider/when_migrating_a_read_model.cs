// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.CodeAnalysis.for_ReadModelCreatorAnalyzer.when_checking_a_possible_fold_creator;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReadModelCreatorAnalyzer, Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.RequireQueryAttributeCodeFixProvider>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_RequireQueryAttributeCodeFixProvider;

public class when_migrating_a_read_model
{
    [Fact]
    public async Task should_opt_in_and_mark_queries_but_not_the_fold() => await VerifyCS.VerifyCodeFixAsync(
        Source("public static Order {|#0:Create|}(Unmarked e) => new();\n        public static Order ById(string id) => new();"),
        Source("public static Order Create(Unmarked e) => new();\n        [Query]\n        public static Order ById(string id) => new();").Replace("[ReadModel]", "[ReadModel(RequireQueryAttribute = true)]", StringComparison.Ordinal),
        Warning());

    [Fact]
    public async Task should_not_reinterpret_a_fold_already_marked_as_a_query() => await VerifyCS.VerifyNoCodeFixAsync(
        Source("[Query] public static Order {|#0:Create|}(Unmarked e) => new();"), Warning());

    [Fact]
    public async Task should_not_introduce_ambiguous_selected_overloads() => await VerifyCS.VerifyNoCodeFixAsync(
        Source("public static Order {|#0:Create|}(Unmarked e) => new();\n        public static Order ById(string id) => new();\n        public static Order ById(int id) => new();"), Warning());

    [Fact]
    public void should_not_offer_unattended_fix_all() => new CodeFixes.RequireQueryAttributeCodeFixProvider().GetFixAllProvider().ShouldBeNull();

    [Fact]
    public async Task should_mark_queries_in_another_partial_declaration() => await VerifyCS.VerifyCodeFixAsync(
        Source("public static Order {|#0:Create|}(Unmarked e) => new();").Replace("public record Order\n", "public partial record Order\n", StringComparison.Ordinal) + "\nnamespace TestApp { public partial record Order { public static Order ById(string id) => new(); } }",
        Source("public static Order Create(Unmarked e) => new();").Replace("public record Order\n", "public partial record Order\n", StringComparison.Ordinal).Replace("[ReadModel]", "[ReadModel(RequireQueryAttribute = true)]", StringComparison.Ordinal) + "\nnamespace TestApp { public partial record Order { [Query] public static Order ById(string id) => new(); } }",
        Warning());
}
