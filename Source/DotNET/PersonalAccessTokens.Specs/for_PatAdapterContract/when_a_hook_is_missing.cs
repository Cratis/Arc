// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.PersonalAccessTokens.Contract;

namespace Cratis.Arc.PersonalAccessTokens.for_PatAdapterContract;

public class when_a_hook_is_missing : Specification
{
    PatContractReport _report;

    async Task Because() => _report = await PatAdapterContract.Run(new PatAdapterHarness(), TestContext.Current.CancellationToken);

    [Fact] void should_report_failure_instead_of_skipping() => _report.Passed.ShouldBeFalse();
    [Fact] void should_name_the_missing_hook() => _report.Results.Single().Name.ShouldEqual("harness");
}
