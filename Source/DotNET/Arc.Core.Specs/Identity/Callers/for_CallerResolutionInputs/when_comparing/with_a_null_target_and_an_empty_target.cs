// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.Callers.for_CallerResolutionInputs.when_comparing;

public class with_a_null_target_and_an_empty_target : Specification
{
    CallerResolutionInputs _null;
    CallerResolutionInputs _empty;

    void Because()
    {
        var request = Substitute.For<IHttpRequestContext, ICanReadForwardedIdentityRequest>();
        request.Method.Returns("GET");
        var raw = (ICanReadForwardedIdentityRequest)request;
        raw.HeaderNames.Returns([]);
        raw.RawTarget.Returns((string)null);
        _null = CallerResolutionInputs.Capture(request, [], null);
        raw.RawTarget.Returns(string.Empty);
        _empty = CallerResolutionInputs.Capture(request, [], null);
    }

    [Fact] void should_distinguish_the_absence_of_a_target() => _null.Matches(_empty).ShouldBeFalse();
}
