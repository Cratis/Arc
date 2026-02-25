// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Queries;
using Cratis.Arc.Swagger.ModelBound;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cratis.Arc.Swagger.for_QueryOperationFilter.given;

public class a_query_operation_filter : Specification
{
    protected IQueryPerformerProviders _queryPerformerProviders;
    protected QueryOperationFilter _filter;
    protected ISchemaGenerator _schemaGenerator;
    protected SchemaRepository _schemaRepository;

    protected record TestReadModel(string Name, int Value);

    void Establish()
    {
        _queryPerformerProviders = Substitute.For<IQueryPerformerProviders>();
        _filter = new QueryOperationFilter(_queryPerformerProviders);
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

    protected IQueryPerformer CreateQueryPerformer(string name, bool supportsPaging, params QueryParameter[] parameters)
    {
        var performer = Substitute.For<IQueryPerformer>();
        performer.Name.Returns(new QueryName(name));
        performer.FullyQualifiedName.Returns(new FullyQualifiedQueryName($"Features.Orders.{name}"));
        performer.Location.Returns(["Features", "Orders"]);
        performer.Type.Returns(typeof(TestReadModel));
        performer.ReadModelType.Returns(typeof(TestReadModel));
        performer.Dependencies.Returns([]);
        performer.Parameters.Returns(new QueryParameters(parameters));
        performer.AllowsAnonymousAccess.Returns(false);
        performer.SupportsPaging.Returns(supportsPaging);
        return performer;
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
