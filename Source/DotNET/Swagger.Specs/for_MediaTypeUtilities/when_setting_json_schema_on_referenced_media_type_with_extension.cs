// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace Cratis.Arc.Swagger.for_MediaTypeUtilities;

public class when_setting_json_schema_on_referenced_media_type_with_extension : Specification
{
    const string ExtensionName = "x-custom";

    OpenApiDocument _document;
    OpenApiMediaType _target;
    OpenApiResponse _response;
    OpenApiSchema _schema;
    IOpenApiMediaType _result;

    void Establish()
    {
        _target = new OpenApiMediaType
        {
            Extensions = new Dictionary<string, IOpenApiExtension>
            {
                [ExtensionName] = new JsonNodeExtension(JsonValue.Create("value"))
            }
        };
        _document = new OpenApiDocument
        {
            Components = new OpenApiComponents
            {
                MediaTypes = new Dictionary<string, IOpenApiMediaType> { ["shared"] = _target }
            }
        };
        _document.RegisterComponents();
        _response = new OpenApiResponse
        {
            Content = new Dictionary<string, IOpenApiMediaType>
            {
                ["application/json"] = new OpenApiMediaTypeReference("shared", _document)
            }
        };
        _schema = new OpenApiSchema { Type = JsonSchemaType.String };
    }

    void Because()
    {
        MediaTypeUtilities.SetJsonSchema(_response, _schema);
        _result = _response.Content["application/json"];
    }

    [Fact] void should_set_the_schema() => _result.Schema.ShouldEqual(_schema);
    [Fact] void should_keep_the_extension() => _result.Extensions.ContainsKey(ExtensionName).ShouldBeTrue();
    [Fact] void should_not_mutate_the_shared_target() => _target.Schema.ShouldBeNull();
    [Fact] void should_keep_the_target_in_components() => _document.Components.MediaTypes["shared"].ShouldEqual(_target);
}
