// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values.given;

public record ContextId(string Value) : ConceptAs<string>(Value), IGeneratableInContext<ContextId>
{
    public static ContextId New(GenerationContext context) => new($"{context.Now:O}|{context.Tenant}");
}
