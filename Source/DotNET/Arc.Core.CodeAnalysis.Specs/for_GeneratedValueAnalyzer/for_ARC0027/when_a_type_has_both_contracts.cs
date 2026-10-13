// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0027;

public class when_a_type_has_both_contracts : given.a_generated_value_analyzer
{
    [Fact]
    public async Task should_report_on_the_parameter() => await VerifyDiagnostic(@"
        public record DualId(string Value) : ConceptAs<string>(Value), IGeneratable<DualId>, IGeneratableInContext<DualId>
        {
            public static DualId New() => new(""plain"");
            public static DualId New(GenerationContext context) => new(context.Tenant);
        }
        [Command] public record Allocate { public void Handle({|#0:[GeneratedValue] DualId id|}) { } }",
        "ARC0027",
        "id",
        "DualId");
}
