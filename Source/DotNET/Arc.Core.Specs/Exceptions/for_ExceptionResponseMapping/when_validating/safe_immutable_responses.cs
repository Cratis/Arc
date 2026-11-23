// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_validating;

public class safe_immutable_responses : Specification
{
    [Theory]
    [InlineData("0")]
    [InlineData("5")]
    [InlineData("Wed, 21 Oct 2015 07:28:00 GMT")]
    void should_accept_safe_retry_after_values(string value)
    {
        var response = new ExceptionResponse(HttpStatusCode.ServiceUnavailable, "safe_code.v1-2", new Dictionary<string, string> { ["retry-after"] = value });
        ExceptionResponseValidation.IsValid(response).ShouldBeTrue();
    }

    [Fact]
    void should_defensively_copy_headers()
    {
        var headers = new Dictionary<string, string> { ["Retry-After"] = "5" };
        var response = new ExceptionResponse(HttpStatusCode.ServiceUnavailable, "safe", headers);
        headers["Retry-After"] = "unsafe";
        response.Headers["Retry-After"].ShouldEqual("5");
        ExceptionResponseValidation.IsValid(response).ShouldBeTrue();
    }

    [Fact]
    void should_not_allow_mutation_through_a_dictionary_cast()
    {
        var response = new ExceptionResponse(HttpStatusCode.ServiceUnavailable, "safe", new Dictionary<string, string> { ["Retry-After"] = "5" });
        var dictionary = (IDictionary<string, string>)response.Headers;
        Catch.Exception(() => dictionary["Retry-After"] = "unsafe").ShouldBeOfExactType<NotSupportedException>();
    }
}
