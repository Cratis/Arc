// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0022;

public class when_read_nullability_matches_runtime : given.a_generated_value_analyzer
{
    [Theory]
    [InlineData("enable", "CartId")]
    [InlineData("enable", "CartId?")]
    [InlineData("disable", "CartId")]
    [InlineData("disable", "CartId?")]
    [InlineData("enable", "[System.Diagnostics.CodeAnalysis.MaybeNull] CartId")]
    [InlineData("disable", "[System.Diagnostics.CodeAnalysis.MaybeNull] CartId")]
    [InlineData("enable", "[System.Diagnostics.CodeAnalysis.NotNull] CartId?")]
    [InlineData("disable", "[System.Diagnostics.CodeAnalysis.NotNull] CartId?")]
    [InlineData("enable", "[System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] CartId")]
    [InlineData("disable", "[System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] CartId")]
    [InlineData("enable", "[System.Diagnostics.CodeAnalysis.NotNullWhen(true)] CartId?")]
    [InlineData("enable", "[System.Diagnostics.CodeAnalysis.AllowNull] CartId")]
    [InlineData("enable", "[System.Diagnostics.CodeAnalysis.DisallowNull] CartId?")]
    [InlineData("enable", "TicketId")]
    [InlineData("enable", "TicketId?")]
    [InlineData("disable", "TicketId?")]
    [InlineData("enable", "[System.Diagnostics.CodeAnalysis.MaybeNull] TicketId")]
    [InlineData("enable", "[System.Diagnostics.CodeAnalysis.NotNull] TicketId?")]
    public async Task should_agree_with_runtime_parameter_validation(string nullableContext, string type) => await VerifyRuntimeParity($@"
#nullable {nullableContext}
        [Command] public record Allocate
        {{
            public void Handle([{{|#0:GeneratedValue|}}] {type} value) {{ }}
        }}");

    [Theory]
    [InlineData("enable", "T", "CartId")]
    [InlineData("enable", "T?", "CartId")]
    [InlineData("disable", "T", "CartId")]
    [InlineData("disable", "T?", "CartId")]
    [InlineData("enable", "[System.Diagnostics.CodeAnalysis.MaybeNull] T", "CartId")]
    [InlineData("enable", "[System.Diagnostics.CodeAnalysis.NotNull] T?", "CartId")]
    [InlineData("enable", "T", "TicketId")]
    [InlineData("enable", "T?", "TicketId")]
    [InlineData("enable", "T", "CartId?")]
    [InlineData("enable", "T?", "CartId?")]
    [InlineData("disable", "T", "CartId?")]
    public async Task should_agree_for_constructed_generic_base_parameters(string nullableContext, string type, string argument) => await VerifyRuntimeParity($@"
#nullable {nullableContext}
        public record Parent<T> where T : IGeneratable<T>
        {{
            public void Handle([{{|#0:GeneratedValue|}}] {type} value) {{ }}
        }}
        [Command] public record Allocate : Parent<{argument}>;");
}
