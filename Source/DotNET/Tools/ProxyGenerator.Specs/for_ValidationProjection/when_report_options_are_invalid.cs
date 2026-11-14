// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_ValidationProjection;

public class when_report_options_are_invalid : Specification
{
    [Theory]
    [InlineData("--validation-report=xml", "")]
    [InlineData("--validation-report", "")]
    [InlineData("--validation-report-file=", "")]
    [InlineData("--check", "--validation-report=text")]
    [InlineData("--check", "--validation-report=json")]
    [InlineData("--check", "--validation-report-file=report.json")]
    [InlineData("--report-validation=json", "")]
    [InlineData("--check-report=json", "")]
    async Task should_fail_with_a_usage_error(string first, string second)
    {
        var result = await ToolProcess.Run("unused.dll", "unused-output", new[] { first, second }.Where(_ => _.Length > 0).ToArray());
        result.ExitCode.ShouldEqual(1);
        result.Error.ShouldContain("error:");
        if (first == "--check") result.Error.ShouldContain("cannot be combined");
    }

    [Theory]
    [InlineData("unused-output")]
    [InlineData("unused-output/report.json")]
    [InlineData("unused-output/nested/../report.json")]
    async Task should_reject_a_report_destination_in_the_proxy_tree(string path)
    {
        var result = await ToolProcess.Run("unused.dll", "unused-output", $"--validation-report-file={path}");
        result.ExitCode.ShouldEqual(1);
        result.Error.ShouldContain("outside the proxy output tree");
    }
}
