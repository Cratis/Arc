// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0025;

public class when_provide_supplies_a_derived_type : given.a_generated_value_analyzer
{
    [Fact] async Task should_warn_for_a_runtime_assignable_provided_value() => await VerifyDiagnostic(
        @"
        public record SpecialCartId(Guid Value) : CartId(Value);
        [Command]
        public record Allocate
        {
            public SpecialCartId Provide() => new(GenerateValue.Uuid());
            public void Handle([{|#0:GeneratedValue|}] CartId value) { }
        }",
        "ARC0025",
        "value",
        "CartId",
        "Allocate");

    [Fact] async Task should_ignore_user_defined_conversions_that_runtime_does_not_apply() => await VerifyValid(@"
        public record Supplied
        {
            public static implicit operator CartId(Supplied value) => CartId.New();
        }
        [Command]
        public record Allocate
        {
            public Supplied Provide() => new();
            public void Handle([GeneratedValue] CartId value) { }
        }
        ");
}
