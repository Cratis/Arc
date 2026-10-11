// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0024;

public class when_names_collide_in_an_inherited_handle : given.a_generated_value_analyzer
{
    [Fact] async Task should_report_the_effective_command_with_duplicate_names() => await VerifyDiagnostic(
        @"
        public record Parent
        {
            public void Handle([GeneratedValue] CartId first, [{|#0:GeneratedValue(""first"")|}] CartId second) { }
        }
        [Command] public record Allocate : Parent;",
        "ARC0024",
        "second",
        "Allocate");
}
