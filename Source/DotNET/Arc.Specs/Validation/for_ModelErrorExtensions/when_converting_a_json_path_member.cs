// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cratis.Arc.Validation.for_ModelErrorExtensions;

public class when_converting_a_json_path_member : Specification
{
    ValidationResult _result;

    void Because() => _result = new ModelError("Invalid value").ToValidationResult("$.Items[2].From");

    [Fact] void should_normalize_the_member() => _result.Members.ShouldContainOnly("items[2].from");
    [Fact] void should_keep_the_formatter_message() => _result.Message.ShouldEqual("Invalid value");
    [Fact] void should_keep_the_malformed_request_reason() => _result.Reason.ShouldEqual(ValidationResultReason.MalformedRequest);
}
