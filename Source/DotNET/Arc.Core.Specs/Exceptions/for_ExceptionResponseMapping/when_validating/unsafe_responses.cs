// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_validating;

public class unsafe_responses : Specification
{
    ExceptionResponse[] _responses;
    bool[] _accepted;

    void Establish() => _responses = [
        new(HttpStatusCode.OK, "safe"),
        new((HttpStatusCode)399, "safe"),
        new((HttpStatusCode)600, "safe"),
        new(HttpStatusCode.Conflict, string.Empty),
        new(HttpStatusCode.Conflict, new string('a', 129)),
        new(HttpStatusCode.Conflict, "unsafe code"),
        new(HttpStatusCode.Conflict, "unsafe\r\ncode"),
        new(HttpStatusCode.Conflict, "nön_ascii"),
        new(HttpStatusCode.ServiceUnavailable, "safe", new Dictionary<string, string> { ["Set-Cookie"] = "not-permitted" }),
        new(HttpStatusCode.ServiceUnavailable, "safe", new Dictionary<string, string> { ["Retry-After"] = "-1" }),
        new(HttpStatusCode.ServiceUnavailable, "safe", new Dictionary<string, string> { ["Retry-After"] = "5\r\nInjected: value" }),
        new(HttpStatusCode.ServiceUnavailable, "safe", new Dictionary<string, string> { ["Retry-After"] = "later" })];

    void Because() => _accepted = _responses.Select(ExceptionResponseValidation.IsValid).ToArray();

    [Fact] void should_reject_every_unsafe_response() => _accepted.ShouldNotContain(true);
}
