// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_ValidationProjection;

public class when_report_options_are_invalid : Specification
{
    [Theory]
    [InlineData("--report-validation=xml", "")]
    [InlineData("--check-report=json", "")]
    [InlineData("--check", "--check-report=xml")]
    [InlineData("--check-report=json", "--report-validation=text")]
    async Task should_fail_with_a_usage_error(string first, string second)
    {
        var result = await ToolProcess.Run("unused.dll", "unused-output", new[] { first, second }.Where(_ => _.Length > 0).ToArray());
        result.ExitCode.ShouldEqual(1);
        result.Error.ShouldContain("error:");
    }
}
