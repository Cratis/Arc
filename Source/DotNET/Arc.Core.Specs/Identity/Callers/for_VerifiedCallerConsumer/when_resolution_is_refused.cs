// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using VerifiedCallerConsumer;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerConsumer;

public class when_resolution_is_refused : Specification
{
    VerifiedCaller? _result;
    bool _policyCalled;

    async Task Because()
    {
        var accessor = Substitute.For<IVerifiedCallerAccessor>();
        accessor.GetCurrent().Returns(new CallerResolution(null, CallerRefusal.NoMatchingSource));
        _result = await new ToolHandler(accessor).Handle(_ => _policyCalled = true);
    }

    [Fact] void should_not_return_a_caller() => _result.ShouldBeNull();
    [Fact] void should_not_treat_missing_verification_as_product_authority() => _policyCalled.ShouldBeFalse();
}
