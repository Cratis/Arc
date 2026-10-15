// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Records an unavailable dependency before a mapping expression is discarded. Never emitted as a value.
/// </summary>
/// <param name="Input">The unavailable generated input.</param>
public record UnavailableValueSource(UnavailableGeneratedValueModel Input) : MappingSourceModel;
