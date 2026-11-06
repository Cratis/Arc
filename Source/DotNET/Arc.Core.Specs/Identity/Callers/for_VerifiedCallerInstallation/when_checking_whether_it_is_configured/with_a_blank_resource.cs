// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerInstallation.when_checking_whether_it_is_configured;

public class with_a_blank_resource : Specification
{
    bool _result;

    void Because() => _result = new VerifiedCallerInstallation(" ", "test")
        .IsConfigured(new CallerResourceCatalog(new Dictionary<string, string> { [" "] = "audience" }));

    [Fact] void should_not_be_configured() => _result.ShouldBeFalse();
}
