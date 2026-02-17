// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.OpenApi.for_ConceptSchemaTransformer.given;

public record TestConcept(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator TestConcept(Guid value) => new(value);
}
