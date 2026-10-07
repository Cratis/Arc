// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.ReadModels;

/// <summary>
/// Represents a read model the application declares, before what it holds has been read.
/// </summary>
/// <param name="Type">The read model type.</param>
/// <param name="Shape">Everything known about the read model but its properties.</param>
public record ReadModelCandidate(INamedTypeSymbol Type, ReadModelModel Shape);
