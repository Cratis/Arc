// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The read-protection section binds into <see cref="ArcOptions.ReadProtection"/> without the reflection fallback.
/// </summary>
[Collection("MutatesTypeDiscovery")]
public class when_binding_read_protection_options : Specification
{
    ArcOptions _options;
    int _bindCount;

    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Arc:ReadProtection:MaximumMode"] = "Measure",
            ["Cratis:Arc:ReadProtection:RequireGeneratedMetadata"] = "true",
            ["Cratis:Arc:ReadProtection:StartupAdmissionCheck"] = "Off",
            ["Cratis:Arc:ReadProtection:MeasureTailTimeout"] = "100",
            ["Cratis:Arc:ReadProtection:MeasureScanTimeout"] = "200",
            ["Cratis:Arc:ReadProtection:MeasureScanLimit"] = "30",
            ["Cratis:Arc:ReadProtection:MeasureSampleRate"] = "0.5"
        }).Build().GetSection("Cratis:Arc");

        RuntimeTypeFallback.ResetBindCount();
        _options = new ArcOptions();
        ArcOptionsBinder.Bind(_options, configuration);
        _bindCount = RuntimeTypeFallback.BindCount;
    }

    [Fact] void should_bind_the_maximum_mode() => _options.ReadProtection.MaximumMode.ShouldEqual("Measure");
    [Fact] void should_bind_the_generated_metadata_requirement() => _options.ReadProtection.RequireGeneratedMetadata.ShouldEqual("true");
    [Fact] void should_bind_the_startup_admission_check() => _options.ReadProtection.StartupAdmissionCheck.ShouldEqual("Off");
    [Fact] void should_bind_the_measure_tail_timeout() => _options.ReadProtection.MeasureTailTimeout.ShouldEqual("100");
    [Fact] void should_bind_the_measure_scan_timeout() => _options.ReadProtection.MeasureScanTimeout.ShouldEqual("200");
    [Fact] void should_bind_the_measure_scan_limit() => _options.ReadProtection.MeasureScanLimit.ShouldEqual("30");
    [Fact] void should_bind_the_measure_sample_rate() => _options.ReadProtection.MeasureSampleRate.ShouldEqual("0.5");
    [Fact] void should_not_run_the_fallback() => _bindCount.ShouldEqual(0);
}
