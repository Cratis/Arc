// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.when_reading_body_as_json;

public class with_a_vendor_json_body : given.a_json_body
{
    void Establish() => _mediaType = "application/vnd.example+json";
    Task Because() => ReadBody();

    [Fact] void should_deserialize_the_body() => _result.Value.ShouldEqual("test");
    [Fact] void should_not_fail() => _exception.ShouldBeNull();
}
