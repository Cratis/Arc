// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http.for_HttpListenerRequestContext.when_reading_body_as_json;

public class without_a_body : given.a_json_body
{
    void Establish()
    {
        _mediaType = "text/plain";
        _body = string.Empty;
    }
    Task Because() => ReadBody();

    [Fact] void should_return_null() => _result.ShouldBeNull();
    [Fact] void should_not_fail() => _exception.ShouldBeNull();
}
