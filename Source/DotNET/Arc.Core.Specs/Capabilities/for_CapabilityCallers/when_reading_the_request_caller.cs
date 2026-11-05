// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Capabilities.for_CapabilityCallers;

public class when_reading_the_request_caller : Specification
{
    CapabilityCaller _caller;
    CapabilityCaller? _withoutRequest;
    CapabilityCaller? _withoutItem;
    CapabilityCaller? _wrongItem;
    CapabilityCaller? _present;

    void Because()
    {
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        var callers = new CapabilityCallers(requests);
        _withoutRequest = callers.Current;
        var request = Substitute.For<IHttpRequestContext>();
        var items = new Dictionary<object, object?>();
        request.Items.Returns(items);
        requests.Current.Returns(request);
        _withoutItem = callers.Current;
        items[CapabilityCallers.ItemKey] = "not a caller";
        _wrongItem = callers.Current;
        _caller = new("group", "rest", CapabilityPrincipalKind.Person, "person", null, null, "credential", null, null, new HashSet<string>());
        items[CapabilityCallers.ItemKey] = _caller;
        _present = callers.Current;
    }

    [Fact] void should_be_null_without_request() => _withoutRequest.ShouldBeNull();
    [Fact] void should_be_null_without_item() => _withoutItem.ShouldBeNull();
    [Fact] void should_be_null_with_wrong_item() => _wrongItem.ShouldBeNull();
    [Fact] void should_return_the_request_caller() => _present.ShouldEqual(_caller);
}
