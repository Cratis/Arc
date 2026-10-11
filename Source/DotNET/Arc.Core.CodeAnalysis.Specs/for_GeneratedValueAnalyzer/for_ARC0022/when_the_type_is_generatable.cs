// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0022;

public class when_the_type_is_generatable : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("CartId")]
    [InlineData("TicketId")]
    [InlineData("ExplicitId")]
    [InlineData("InheritedId")]
    public async Task should_accept_self_generatable_types(string type) => await VerifyValid($@"
        public record ExplicitId : IGeneratable<ExplicitId>
        {{
            static ExplicitId IGeneratable<ExplicitId>.New() => new();
        }}
        public record Generatable<T> : IGeneratable<T> where T : IGeneratable<T>
        {{
            public static T New() => T.New();
        }}
        public record InheritedId : Generatable<InheritedId>;
        [Command]
        public record Allocate
        {{
            public void Handle([GeneratedValue] {type} value) {{ }}
        }}");
}
