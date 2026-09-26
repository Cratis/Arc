// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Arc.Http;
using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_http_collection_arguments : Specification
{
    public record TestReadModel(HashSet<int> Ids, IEnumerable<DateOnly> Dates, IEnumerable<TimeOnly> Times, IEnumerable<Uri> Uris, IEnumerable<int?> OptionalIds)
    {
        public static TestReadModel Query(HashSet<int> ids, DateOnly[] dates, IEnumerable<TimeOnly> times, IEnumerable<Uri> uris, int?[] optionalIds) =>
            new(ids, dates, times, uris, optionalIds);

        public static TestReadModel Nested(int[][] ids) => new([], [], [], [], []);

        public static IEnumerable<JsonNode> Json(IEnumerable<JsonNode> nodes) => nodes;

        public static IEnumerable<JsonObject> JsonObjects(IEnumerable<JsonObject> objects) => objects;

        public static IEnumerable<JsonArray> JsonArrays(IEnumerable<JsonArray> arrays) => arrays;
    }

    static ModelBoundQueryPerformer Performer(string method = nameof(TestReadModel.Query))
    {
        var isService = Substitute.For<Microsoft.Extensions.DependencyInjection.IServiceProviderIsService>();
        isService.IsService(Arg.Any<Type>()).Returns(true);
        return new ModelBoundQueryPerformer(
            typeof(TestReadModel),
            typeof(TestReadModel).FullName!,
            typeof(TestReadModel).GetMethod(method)!,
            isService,
            Substitute.For<Cratis.Arc.Authorization.IAuthorizationEvaluator>());
    }

    static async Task<QueryArguments> ReadGet(ModelBoundQueryPerformer performer, IReadOnlyDictionary<string, string> values)
    {
        var context = Substitute.For<IHttpRequestContext>();
        context.Query.Returns(values);
        return (await new QueryStringQueryRequestReader().Read(context, performer)).Arguments;
    }

    static async Task<QueryArguments> ReadBody(ModelBoundQueryPerformer performer, Dictionary<string, JsonElement> values)
    {
        var context = Substitute.For<IHttpRequestContext>();
        context.ReadBodyAsJson(typeof(QueryRequestEnvelope), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new QueryRequestEnvelope { Arguments = values }));
        return (await new BodyQueryRequestReader().Read(context, performer)).Arguments;
    }

    static async Task<TestReadModel> Perform(ModelBoundQueryPerformer performer, QueryArguments arguments)
    {
        var context = new QueryContext(
            performer.FullyQualifiedName,
            CorrelationId.New(),
            Paging.NotPaged,
            Sorting.None,
            arguments,
            []);
        return (TestReadModel)(await performer.Perform(context))!;
    }

    [Fact]
    async Task should_bind_matching_values_from_get_and_query_body()
    {
        var performer = Performer();
        performer.Dependencies.ShouldBeEmpty();
        var get = await ReadGet(performer, new Dictionary<string, string>
        {
            ["ids"] = "1,2", ["dates"] = "2026-05-12", ["times"] = "14:30:45",
            ["uris"] = "https://example.com/a", ["optionalIds"] = "3,4"
        });
        var body = await ReadBody(performer, new()
        {
            ["ids"] = JsonSerializer.SerializeToElement(new[] { 1, 2 }),
            ["dates"] = JsonSerializer.SerializeToElement(new[] { "2026-05-12" }),
            ["times"] = JsonSerializer.SerializeToElement(new[] { "14:30:45" }),
            ["uris"] = JsonSerializer.SerializeToElement(new[] { "https://example.com/a" }),
            ["optionalIds"] = JsonSerializer.SerializeToElement(new int?[] { 3, 4 })
        });

        foreach (var arguments in new[] { get, body })
        {
            var result = await Perform(performer, arguments);
            result.Ids.SetEquals([1, 2]).ShouldBeTrue();
            result.Dates.ShouldContain(new DateOnly(2026, 5, 12));
            result.Times.Single().ShouldEqual(new TimeOnly(14, 30, 45));
            result.Uris.Single().ToString().ShouldEqual("https://example.com/a");
            result.OptionalIds.SequenceEqual([3, 4]).ShouldBeTrue();
        }
    }

    [Fact]
    async Task should_preserve_nullable_nulls_from_body()
    {
        var performer = Performer();
        var arguments = await ReadBody(performer, new() { ["optionalIds"] = JsonSerializer.SerializeToElement(new int?[] { 1, null, 3 }) });
        ((int?[])arguments["optionalIds"]).SequenceEqual([1, null, 3]).ShouldBeTrue();
    }

    [Fact]
    async Task should_reject_invalid_elements_in_both_transports()
    {
        var performer = Performer();
        var getError = await Catch.Exception(() => ReadGet(performer, new Dictionary<string, string> { ["ids"] = "1,wrong" }));
        var bodyError = await Catch.Exception(() => ReadBody(performer, new() { ["ids"] = JsonSerializer.SerializeToElement(new object[] { 1, "wrong" }) }));
        (getError is IValidationFailure).ShouldBeTrue();
        (bodyError is IValidationFailure).ShouldBeTrue();
    }

    [Fact]
    async Task should_reject_null_non_nullable_elements()
    {
        var error = await Catch.Exception(() => ReadBody(Performer(), new() { ["ids"] = JsonSerializer.SerializeToElement(new int?[] { 1, null }) }));
        (error is IValidationFailure).ShouldBeTrue();
    }

    [Fact]
    async Task should_bind_json_nodes_instead_of_injecting_them()
    {
        var performer = Performer(nameof(TestReadModel.Json));
        performer.Dependencies.ShouldBeEmpty();
        var arguments = await ReadBody(performer, new()
        {
            ["nodes"] = JsonSerializer.SerializeToElement(new object[] { new { id = 1 }, new[] { 2, 3 } })
        });
        var context = new QueryContext(performer.FullyQualifiedName, CorrelationId.New(), Paging.NotPaged, Sorting.None, arguments, []);
        var nodes = (IEnumerable<JsonNode>)(await performer.Perform(context))!;
        nodes.First()["id"]!.GetValue<int>().ShouldEqual(1);
        nodes.Last().AsArray().Count.ShouldEqual(2);
    }

    [Fact]
    async Task should_bind_json_objects_and_arrays_through_model_bound_queries()
    {
        var objectsPerformer = Performer(nameof(TestReadModel.JsonObjects));
        var arraysPerformer = Performer(nameof(TestReadModel.JsonArrays));
        objectsPerformer.Dependencies.ShouldBeEmpty();
        arraysPerformer.Dependencies.ShouldBeEmpty();
        var objectsArguments = await ReadBody(objectsPerformer, new()
        {
            ["objects"] = JsonSerializer.SerializeToElement(new[] { new { id = 1 }, new { id = 2 } })
        });
        var arraysArguments = await ReadBody(arraysPerformer, new()
        {
            ["arrays"] = JsonSerializer.SerializeToElement(new int[][] { [1, 2], [3, 4] })
        });

        var objectsContext = new QueryContext(objectsPerformer.FullyQualifiedName, CorrelationId.New(), Paging.NotPaged, Sorting.None, objectsArguments, []);
        var arraysContext = new QueryContext(arraysPerformer.FullyQualifiedName, CorrelationId.New(), Paging.NotPaged, Sorting.None, arraysArguments, []);
        var objects = (IEnumerable<JsonObject>)(await objectsPerformer.Perform(objectsContext))!;
        var arrays = (IEnumerable<JsonArray>)(await arraysPerformer.Perform(arraysContext))!;
        objects.Select(node => node["id"]!.GetValue<int>()).SequenceEqual([1, 2]).ShouldBeTrue();
        arrays.Last()[1]!.GetValue<int>().ShouldEqual(4);
    }

    [Fact]
    async Task should_reject_nested_arrays_even_if_registered_as_a_service()
    {
        var performer = Performer(nameof(TestReadModel.Nested));
        performer.Dependencies.ShouldBeEmpty();
        var error = await Catch.Exception(() => ReadBody(performer, new() { ["ids"] = JsonSerializer.SerializeToElement(new[] { new[] { 1, 2 } }) }));
        (error is IValidationFailure).ShouldBeTrue();
    }
}
