// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0025;

public class when_handle_overrides_a_generated_parameter : given.a_generated_value_analyzer
{
    [Fact] async Task should_report_a_provided_generated_type_at_the_overriding_parameter() => await VerifyDiagnostic(
        @"
        public record Parent
        {
            public virtual void Handle([GeneratedValue] CartId original) { }
        }
        [Command] public record Allocate : Parent
        {
            public CartId Provide() => CartId.New();
            public override void Handle(CartId {|#0:value|}) { }
        }",
        "ARC0025",
        "value",
        "CartId",
        "Allocate");
}
