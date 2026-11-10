// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.Authorization;
using Cratis.Arc.Mcp.for_McpToolMapper.given;
using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ControllerBased;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Json;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Mcp.for_McpToolMapper;

public class when_query_parameters_have_descriptions : Specification
{
    JsonNode _model;
    JsonNode _controller;
    JsonNode _ambiguous;

    void Because()
    {
        var method = typeof(DescribedQueries).GetMethod(nameof(DescribedQueries.Search))!;
        var serviceTypes = Substitute.For<IServiceProviderIsService>();
        var evaluator = Substitute.For<IAuthorizationEvaluator>();
        var model = new ModelBoundQueryPerformer(typeof(DescribedQueries), nameof(DescribedQueries), method, serviceTypes, evaluator);
        var action = new ControllerActionDescriptor { ControllerTypeInfo = typeof(DescribedQueries).GetTypeInfo(), MethodInfo = method };
        var controller = new ControllerQueryPerformer(action, serviceTypes, evaluator);
        _model = Schema(model);
        _controller = Schema(controller);
        var ambiguous = Substitute.For<IQueryPerformer>();
        ambiguous.Type.Returns(typeof(DescribedQueries));
        ambiguous.ReadModelType.Returns(typeof(DescribedQueries));
        ambiguous.Name.Returns(new QueryName(nameof(DescribedQueries.Ambiguous)));
        ambiguous.FullyQualifiedName.Returns(new FullyQualifiedQueryName($"{typeof(DescribedQueries).FullName}.{nameof(DescribedQueries.Ambiguous)}"));
        ambiguous.Parameters.Returns(new QueryParameters([new QueryParameter("name", typeof(string))]));
        _ambiguous = Schema(ambiguous);
    }

    [Fact] void should_describe_model_bound_arguments_under_the_wire_name() => _model["customer_id"]!["description"]!.GetValue<string>().ShouldEqual("The customer to find.");
    [Fact] void should_describe_controller_arguments_under_the_wire_name() => _controller["customer_id"]!["description"]!.GetValue<string>().ShouldEqual("The customer to find.");
    [Fact] void should_keep_enum_argument_descriptions() => _model["priority"]!["description"]!.GetValue<string>().ShouldEqual("The requested priority.");
    [Fact] void should_omit_blank_descriptions() => _model["blank"]!["description"].ShouldBeNull();
    [Fact] void should_not_invent_descriptions() => _controller["undescribed"]!["description"].ShouldBeNull();
    [Fact] void should_not_guess_between_ambiguous_overloads() => _ambiguous["name"]!["description"].ShouldBeNull();

    static JsonNode Schema(IQueryPerformer performer)
    {
        var serializerOptions = new JsonSerializerOptions().ConfigureArcDefaults();
        serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        var declaration = new McpExposureDeclaration(McpToolKind.Query, performer.FullyQualifiedName.Value, "search", "Searches.");
        return JsonNode.Parse(McpToolMapper.Map(declaration, new McpOptions { ApplicationName = "app" }, [], [performer], serializerOptions).InputSchema!)!["properties"]!;
    }
}
