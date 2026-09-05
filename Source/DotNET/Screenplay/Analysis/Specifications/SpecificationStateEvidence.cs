// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Represents the exact source artifact and location establishing one specification state step.
/// </summary>
/// <param name="Artifact">The exact event, read model, or command symbol.</param>
/// <param name="Source">The exact authored step location.</param>
internal sealed record SpecificationStateEvidence(ITypeSymbol Artifact, Location Source);
