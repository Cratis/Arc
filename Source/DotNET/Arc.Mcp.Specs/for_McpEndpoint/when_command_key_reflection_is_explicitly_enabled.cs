// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Arc.Commands;
using Cratis.Arc.Mcp.for_McpEndpoint.given;
using Microsoft.AspNetCore.Builder;

namespace Cratis.Arc.Mcp.for_McpEndpoint;

public class when_command_key_reflection_is_explicitly_enabled : a_missing_metadata_command_host
{
    protected override void ConfigureEndpoints(WebApplication app) => app.Configuration["ReadProtection:RequireGeneratedMetadata"] = "false";

    async Task Because()
    {
        await SeedStock();
        await Reserve();
    }

    [Fact(Skip = "Reflection compatibility requires dynamic code support.", SkipType = typeof(RuntimeFeature), SkipUnless = nameof(RuntimeFeature.IsDynamicCodeSupported))]
    void should_succeed_through_mcp() => _response.IsToolError.ShouldBeFalse();

    [Fact(Skip = "Reflection compatibility requires dynamic code support.", SkipType = typeof(RuntimeFeature), SkipUnless = nameof(RuntimeFeature.IsDynamicCodeSupported))]
    void should_execute_the_handler_once() => _decision.Handles.ShouldEqual(1);

    [Fact(Skip = "Reflection compatibility requires dynamic code support.", SkipType = typeof(RuntimeFeature), SkipUnless = nameof(RuntimeFeature.IsDynamicCodeSupported))]
    void should_commit_the_protected_decision() => _appended.OfType<StockReserved>().Single().Available.ShouldEqual(4);

    [Fact(Skip = "Reflection compatibility requires dynamic code support.", SkipType = typeof(RuntimeFeature), SkipUnless = nameof(RuntimeFeature.IsDynamicCodeSupported))]
    void should_keep_read_protection_on() => _decision.ReadProtection.ShouldEqual(ReadProtectionMode.On);

    [Fact(Skip = "Reflection compatibility requires dynamic code support.", SkipType = typeof(RuntimeFeature), SkipUnless = nameof(RuntimeFeature.IsDynamicCodeSupported))]
    void should_report_the_explicit_compatibility_path() =>
        _logs.Entries.Any(entry => entry.Content.Contains("uses reflection compatibility for command read-model key lookup", StringComparison.Ordinal)).ShouldBeTrue();
}
