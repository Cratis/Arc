// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.for_ConverterExtensions;

#pragma warning disable SA1402, SA1649 // File may only contain a single type, file name must match first type name

public record Count(int Value) : ConceptAs<int>(Value);

public enum Color
{
    Red = 0,
    Green = 1
}

#pragma warning restore SA1402, SA1649
