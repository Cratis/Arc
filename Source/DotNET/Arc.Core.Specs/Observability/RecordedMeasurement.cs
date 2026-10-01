// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Observability;

/// <summary>
/// A measurement recorded on an instrument while a spec ran.
/// </summary>
/// <param name="Instrument">The name of the instrument.</param>
/// <param name="Value">The value recorded.</param>
/// <param name="Tags">The tags recorded with it.</param>
public record RecordedMeasurement(string Instrument, double Value, IReadOnlyDictionary<string, object?> Tags);
