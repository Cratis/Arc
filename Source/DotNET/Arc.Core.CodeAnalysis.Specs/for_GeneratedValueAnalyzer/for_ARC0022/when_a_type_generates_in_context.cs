// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.CodeAnalysis.for_GeneratedValueAnalyzer.for_ARC0022;

public class when_a_type_generates_in_context : given.a_generated_value_analyzer
{
    [Fact]
    public async Task should_accept_the_context_contract() => await VerifyValid(@"
        public record ContextId(string Value) : ConceptAs<string>(Value), IGeneratableInContext<ContextId>
        {
            static ContextId IGeneratableInContext<ContextId>.New(GenerationContext context) => new(context.Tenant);
        }
        [Command] public record Allocate { public void Handle([GeneratedValue] ContextId id) { } }");
}
