// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

/// <summary>
/// The namespaced Measure keys win over the root keys: a root sample rate of 0 would never scan.
/// </summary>
public class when_scanning_with_namespaced_measure_keys : when_scanning_after_own_append
{
    void Establish() => _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ReadProtection:MeasureSampleRate"] = "0",
        ["Cratis:Arc:ReadProtection:MeasureSampleRate"] = "1",
        ["Cratis:Arc:ReadProtection:MeasureTailTimeout"] = "5000"
    }).Build();

    [Fact] void should_scan_using_the_namespaced_sample_rate() => _reason.ShouldContain("at 101 would have conflicted");
}
