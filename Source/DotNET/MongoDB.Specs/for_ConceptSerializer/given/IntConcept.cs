// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.MongoDB.for_ConceptSerializer.given;

public record IntConcept(int Value) : ConceptAs<int>(Value);
