// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0025;

public class when_handle_is_inherited : given.a_generated_value_analyzer
{
    [Fact] async Task should_match_provide_on_the_effective_command() => await VerifyDiagnostic(
        @"
        public record Parent { public void Handle([{|#0:GeneratedValue|}] CartId value) { } }
        [Command] public record Allocate : Parent { private CartId Provide() => CartId.New(); }
        [Command] public record Other : Parent { public TicketId Provide() => TicketId.New(); }",
        "ARC0025",
        "value",
        "CartId",
        "Allocate");

    [Fact] async Task should_use_a_derived_provider_hiding_a_different_base_return_type() => await VerifyDiagnostic(
        @"
        public record Parent
        {
            public TicketId Provide() => TicketId.New();
            public void Handle([{|#0:GeneratedValue|}] CartId value) { }
        }
        [Command] public record Allocate : Parent { public new CartId Provide() => CartId.New(); }",
        "ARC0025",
        "value",
        "CartId",
        "Allocate");

    [Fact] async Task should_not_use_a_base_provider_hidden_by_a_derived_provider() => await VerifyValid(@"
        public record Parent
        {
            public CartId Provide() => CartId.New();
            public void Handle([GeneratedValue] CartId value) { }
        }
        [Command] public record Allocate : Parent { public new TicketId Provide() => TicketId.New(); }");
}
