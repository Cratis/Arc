// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Verify = Cratis.Arc.CodeAnalysis.Specs.Testing.AnalyzerVerifier<Cratis.Arc.CodeAnalysis.CommandOperationAnalyzer>;

namespace Cratis.Arc.CodeAnalysis.for_CommandOperationAnalyzer;

public class when_a_bare_operation_collection_is_returned
{
    [Fact] public async Task should_direct_the_author_to_the_explicit_batch() => await Verify.VerifyAnalyzerAsync(
        @"
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        [Command]
        public record Invalid
        {
            public ICommandOperation[] {|#0:Handle|}() => [];
        }",
        Verify.Diagnostic("ARC0017").WithLocation(0));

    [Theory]
    [InlineData("Operation?[]")]
    [InlineData("CommandOperations?[]")]
    public async Task should_reject_nullable_value_operation_collections(string returnType) => await Verify.VerifyAnalyzerAsync(
        @"
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        public readonly record struct Operation : ICommandOperation
        {
            public void Execute() { }
        }
        [Command]
        public record Invalid
        {
            public " + returnType + @" {|#0:Handle|}() => [];
        }",
        Verify.Diagnostic("ARC0017").WithLocation(0));

    [Fact] public async Task should_preserve_ordinary_collection_responses() => await Verify.VerifyAnalyzerAsync(@"
        using Cratis.Arc.Commands;
        using Cratis.Arc.Commands.ModelBound;
        [Command]
        public record Valid
        {
            public (string[], CommandOperations) Handle() => ([], []);
        }");
}
