// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.when_reading_body_as_json;

public class with_a_text_plain_body : given.a_json_body
{
    void Establish() => _mediaType = "text/plain";
    Task Because() => ReadBody();

    [Fact] void should_reject_the_content_type() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_deserialize_the_body() => _result.ShouldBeNull();
}
