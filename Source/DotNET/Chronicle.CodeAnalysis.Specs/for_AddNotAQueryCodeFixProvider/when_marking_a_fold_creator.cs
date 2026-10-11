// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static Cratis.Arc.Chronicle.CodeAnalysis.for_ReadModelCreatorAnalyzer.when_checking_a_possible_fold_creator;

using VerifyCS = Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing.CodeFixVerifier<Cratis.Arc.Chronicle.CodeAnalysis.ReadModelCreatorAnalyzer, Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes.AddNotAQueryCodeFixProvider>;

namespace Cratis.Arc.Chronicle.CodeAnalysis.for_AddNotAQueryCodeFixProvider;

public class when_marking_a_fold_creator
{
    [Fact]
    public async Task should_add_the_opt_out_attribute() => await VerifyCS.VerifyCodeFixAsync(
        Source("public static Order {|#0:Create|}(Unmarked e) => new();"),
        Source("[NotAQuery]\n        public static Order Create(Unmarked e) => new();"),
        Warning());
}
