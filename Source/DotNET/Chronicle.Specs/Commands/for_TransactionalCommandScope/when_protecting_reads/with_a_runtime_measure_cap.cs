// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_protecting_reads;

public class with_a_runtime_measure_cap : given.a_read_protection_pipeline
{
    ReadProtectionMode _effective;
    bool _enforcing;

    void Establish()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ReadProtection:MaximumMode"] = "Measure" }).Build();
        _services.GetService(typeof(IConfiguration)).Returns(configuration);
        _handle = _ =>
        {
            _effective = CommandDecisionPolicy.ReadProtection;
            _enforcing = _unit.IsEnforcingReadProtection;
            return Task.FromResult<object?>("unchanged");
        };
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_cap_the_pipeline_policy() => _effective.ShouldEqual(ReadProtectionMode.Measure);
    [Fact] void should_not_install_enforcement() => _enforcing.ShouldBeFalse();
    [Fact] void should_preserve_the_handler_response() => ((CommandResult<string>)_result).Response.ShouldEqual("unchanged");
    [Fact] void should_preserve_success() => _result.IsSuccess.ShouldBeTrue();
}
