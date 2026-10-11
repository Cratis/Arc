// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0023;

public class when_handle_is_not_used_by_a_command : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("public abstract record CreateBase")]
    [InlineData("public sealed record CreateBase")]
    [InlineData("internal record CreateBase")]
    [InlineData("public readonly record struct CreateBase")]
    public async Task should_leave_the_unused_attribute_inert(string declaration) => await VerifyValid($@"
        {declaration}
        {{
            public void Handle([GeneratedValue] CartId value) {{ }}
        }}");

    [Fact] async Task should_ignore_a_handle_on_a_type_with_only_a_private_constructor() => await VerifyValid(@"
        public class CreateBase
        {
            private CreateBase() { }
            public void Handle([GeneratedValue] CartId value) { }
        }");
}
