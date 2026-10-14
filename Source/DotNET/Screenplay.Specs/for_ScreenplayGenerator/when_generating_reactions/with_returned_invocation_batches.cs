// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions.given;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating_reactions;

public class with_returned_invocation_batches : a_refusing_application
{
    [Theory]
    [InlineData("IEnumerable<CommandInvocation>", "new[] {", "}")]
    [InlineData("IEnumerable<object>", "[", "]")]
    [InlineData("Task<CommandInvocation[]>", "Task.FromResult(new[] {", "})")]
    public void should_keep_complete_invocation_batches_in_order(string returnType, string prefix, string suffix)
    {
        GenerateWith($$"""
            public class Welcomer : IReactor
            {
                public {{returnType}} Welcome(AuthorRegistered @event) => {{prefix}}
                    {{Invocation}}.OnRefused(Refused.ByValidation, refusal => new AuthorFiled(refusal.Message)),
                    {{Invocation}}.Acknowledge(Refused.Any)
                {{suffix}};
            }
            """);
        Result.Model.Slices.SelectMany(slice => slice.Reactors).Single().Reactions.Single().Invokes.Count().ShouldEqual(2);
        AssertRefusalExtraction();
    }

    [Fact]
    public void should_keep_the_whole_batch_file_backed_if_a_later_callback_is_opaque()
    {
        GenerateWith($$"""
            public class Welcomer : IReactor
            {
                public IEnumerable<CommandInvocation> Welcome(AuthorRegistered @event) => [
                    {{Invocation}}.Acknowledge(Refused.Any),
                    {{Invocation}}.OnRefused(Refused.Any, refusal => { Console.WriteLine(refusal.Message); return null; })
                ];
            }
            """);
        AssertRefusalFallback();
    }

    [Fact]
    public void should_read_an_async_handler_returning_an_invocation_directly()
    {
        GenerateWith($$"""
            public class Welcomer : IReactor
            {
                public async Task<CommandInvocation> Welcome(AuthorRegistered @event) => {{Invocation}}.Acknowledge(Refused.Any);
            }
            """);
        AssertRefusalExtraction();
    }
}
