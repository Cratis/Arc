// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// The names Arc publishes its telemetry under, for registering with OpenTelemetry or any other listener.
/// </summary>
public static class WellKnownDiagnostics
{
    /// <summary>
    /// The name of the activity source Arc's spans are raised on.
    /// </summary>
    public const string ActivitySourceName = "Cratis.Arc";

    /// <summary>
    /// The name of the meter Arc's metrics are recorded on.
    /// </summary>
    public const string MeterName = "Cratis.Arc";
}
