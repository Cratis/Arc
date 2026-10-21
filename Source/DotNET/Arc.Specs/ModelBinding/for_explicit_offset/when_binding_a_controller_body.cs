// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.ModelBinding.for_explicit_offset;

public class when_binding_a_controller_body : Specification
{
    InputFormatterResult _result;
    ModelStateDictionary _state;
    ValidationResult _validation;

    async Task Because()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("""{"from":"2026-10-16T12:30"}"""));
        _state = new ModelStateDictionary();
        var metadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(ControllerBody));
        var formatter = new SystemTextJsonInputFormatter(new JsonOptions(), NullLogger<SystemTextJsonInputFormatter>.Instance);
        _result = await formatter.ReadAsync(new InputFormatterContext(context, string.Empty, _state, metadata, (stream, encoding) => new StreamReader(stream, encoding)));
        _validation = _state["$.from"]!.Errors.Single().ToValidationResult("$.from");
    }

    [Fact] void should_refuse_binding() => _result.HasError.ShouldBeTrue();
    [Fact] void should_use_the_json_path_as_the_formatter_key() => _state.Keys.ShouldContainOnly("$.from");
    [Fact] void should_normalize_the_validation_member() => _validation.Members.ShouldContainOnly("from");
    [Fact] void should_keep_the_formatter_refusal_message() => _validation.Message.ShouldEqual("The value requires an explicit offset or Z.");

    public record ControllerBody([property: ExplicitOffset] DateTimeOffset From);
}
