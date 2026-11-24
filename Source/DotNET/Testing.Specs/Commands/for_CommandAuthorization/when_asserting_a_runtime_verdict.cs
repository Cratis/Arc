// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization;

public class when_asserting_a_runtime_verdict : given.an_evaluation
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    async Task should_reject_the_other_outcomes(bool allowed)
    {
        var result = await CommandAuthorization.For(Provider, Principal, new given.ResourceCommand(allowed), TestContext.Current.CancellationToken);
        if (allowed)
        {
            result.ShouldBeAuthorized();
            Assert.Throws<CommandAuthorizationAssertionFailed>(result.ShouldBeDenied);
        }
        else
        {
            result.ShouldBeDenied();
            Assert.Throws<CommandAuthorizationAssertionFailed>(result.ShouldBeAuthorized);
        }

        Assert.Throws<CommandAuthorizationAssertionFailed>(result.ShouldBeUnsupported);
    }
}
