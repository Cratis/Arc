// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.Callers.for_CallerResolutionInputs.when_comparing;

public class with_reframed_values : Specification
{
    CallerResolutionInputs _first;
    CallerResolutionInputs _same;
    CallerResolutionInputs _reframed;
    void Because()
    {
        var request = Substitute.For<IHttpRequestContext, ICanReadForwardedIdentityRequest>();
        request.Method.Returns("POST");
        var raw = (ICanReadForwardedIdentityRequest)request;
        raw.RawTarget.Returns("/mcp");
        raw.HeaderNames.Returns(["authorization"]);
        raw.GetHeaderValues("authorization").Returns(["a", "bc"]);
        _first = CallerResolutionInputs.Capture(request, [], null);
        _same = CallerResolutionInputs.Capture(request, [], null);
        raw.GetHeaderValues("authorization").Returns(["ab", "c"]);
        _reframed = CallerResolutionInputs.Capture(request, [], null);
    }
    [Fact] void should_match_identical_inputs() => _first.Matches(_same).ShouldBeTrue();
    [Fact] void should_distinguish_value_boundaries() => _first.Matches(_reframed).ShouldBeFalse();
}
