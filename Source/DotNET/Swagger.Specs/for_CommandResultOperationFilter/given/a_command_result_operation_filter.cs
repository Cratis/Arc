// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cratis.Arc.Swagger.for_CommandResultOperationFilter.given;

public class a_command_result_operation_filter : Specification
{
    protected CommandResultOperationFilter _filter;
    protected ISchemaGenerator _schemaGenerator;
    protected OpenApiSchema _schema;

    public class TestController
    {
        [HttpPost]
        public Task Execute() => Task.CompletedTask;

        public void NotACommand()
        {
        }
    }

    void Establish()
    {
        _filter = new CommandResultOperationFilter();
        _schema = new OpenApiSchema { Type = JsonSchemaType.Object };
        _schemaGenerator = Substitute.For<ISchemaGenerator>();
        _schemaGenerator.GenerateSchema(Arg.Any<Type>(), Arg.Any<SchemaRepository>()).Returns(_schema);
    }

    protected OpenApiOperation CreateOperation(IDictionary<string, IOpenApiMediaType>? content) =>
        new()
        {
            Responses = new OpenApiResponses { ["200"] = new OpenApiResponse { Description = "OK", Content = content } }
        };

    protected OperationFilterContext CreateFilterContext(string methodName) =>
        new(
            new Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription(),
            _schemaGenerator,
            new SchemaRepository(),
            new OpenApiDocument(),
            typeof(TestController).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance)!);

    protected static JsonNode Example => JsonValue.Create("example")!;
}
