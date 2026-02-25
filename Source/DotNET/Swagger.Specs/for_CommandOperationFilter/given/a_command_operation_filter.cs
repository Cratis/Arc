// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Swagger.ModelBound;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cratis.Arc.Swagger.for_CommandOperationFilter.given;

public class a_command_operation_filter : Specification
{
    protected ICommandHandlerProviders _commandHandlerProviders;
    protected CommandOperationFilter _filter;
    protected ISchemaGenerator _schemaGenerator;
    protected SchemaRepository _schemaRepository;

    protected record TestCommand(string Name, int Value);
    protected record AnotherCommand(string Data);

    void Establish()
    {
        _commandHandlerProviders = Substitute.For<ICommandHandlerProviders>();
        _filter = new CommandOperationFilter(_commandHandlerProviders);
        _schemaGenerator = Substitute.For<ISchemaGenerator>();
        _schemaRepository = new SchemaRepository();

        _schemaGenerator.GenerateSchema(Arg.Any<Type>(), Arg.Any<SchemaRepository>())
            .Returns(new OpenApiSchema { Type = JsonSchemaType.Object });
    }

    protected OpenApiOperation CreateOperation(string operationId)
    {
        return new OpenApiOperation
        {
            OperationId = operationId,
            Responses = new OpenApiResponses(),
            Parameters = []
        };
    }

    protected ICommandHandler CreateCommandHandler(Type commandType)
    {
        var handler = Substitute.For<ICommandHandler>();
        handler.CommandType.Returns(commandType);
        handler.Location.Returns(["Features", "Orders"]);
        handler.Dependencies.Returns([]);
        handler.AllowsAnonymousAccess.Returns(false);
        return handler;
    }

    protected OperationFilterContext CreateFilterContext()
    {
        return new OperationFilterContext(
            new Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription(),
            _schemaGenerator,
            _schemaRepository,
            new OpenApiDocument(),
            typeof(object).GetMethods(BindingFlags.Public | BindingFlags.Instance)[0]);
    }
}
