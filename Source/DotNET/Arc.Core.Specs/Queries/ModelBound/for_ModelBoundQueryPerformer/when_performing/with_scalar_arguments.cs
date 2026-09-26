// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Http;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_performing;

public class with_scalar_arguments : Specification
{
    public enum Status
    {
        Active = 0,
        Inactive = 1
    }

    public record TestReadModel(int Count, Guid Id, DateOnly Date, Status State, bool Enabled)
    {
        public static bool WasCalled { get; set; }

        public static TestReadModel Query(int count, Guid id, DateOnly date, Status state, bool enabled)
        {
            WasCalled = true;
            return new(count, id, date, state, enabled);
        }
    }

    public record AccountId(Guid Value) : ConceptAs<Guid>(Value);

    public record ConceptReadModel
    {
        public static bool WasCalled { get; set; }

        public static ConceptReadModel Query(AccountId required, AccountId? optional = null, AccountId defaulted = null!)
        {
            WasCalled = true;
            return new ConceptReadModel();
        }
    }

    static ModelBoundQueryPerformer ConceptPerformer() => new(
        typeof(ConceptReadModel),
        typeof(ConceptReadModel).FullName!,
        typeof(ConceptReadModel).GetMethod(nameof(ConceptReadModel.Query))!,
        Substitute.For<IServiceProviderIsService>(),
        Substitute.For<Cratis.Arc.Authorization.IAuthorizationEvaluator>());

    static ModelBoundQueryPerformer Performer() => new(
        typeof(TestReadModel),
        typeof(TestReadModel).FullName!,
        typeof(TestReadModel).GetMethod(nameof(TestReadModel.Query))!,
        Substitute.For<IServiceProviderIsService>(),
        Substitute.For<Cratis.Arc.Authorization.IAuthorizationEvaluator>());

    static async Task<QueryArguments> ReadGet(ModelBoundQueryPerformer performer, Dictionary<string, string> values)
    {
        var context = Substitute.For<IHttpRequestContext>();
        context.Query.Returns(values);
        return (await new QueryStringQueryRequestReader().Read(context, performer)).Arguments;
    }

    static async Task<QueryArguments> ReadBody(ModelBoundQueryPerformer performer, Dictionary<string, string> values)
    {
        var context = Substitute.For<IHttpRequestContext>();
        context.ReadBodyAsJson(typeof(QueryRequestEnvelope), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<object?>(new QueryRequestEnvelope
            {
                Arguments = values.ToDictionary(pair => pair.Key, pair => JsonSerializer.SerializeToElement(pair.Value))
            }));
        return (await new BodyQueryRequestReader().Read(context, performer)).Arguments;
    }

    static async Task<TestReadModel> Perform(ModelBoundQueryPerformer performer, QueryArguments arguments) =>
        (TestReadModel)(await performer.Perform(new QueryContext(
            performer.FullyQualifiedName, CorrelationId.New(), Paging.NotPaged, Sorting.None, arguments, [])))!;

    [Theory]
    [InlineData("count", "abc")]
    [InlineData("id", "not-a-guid")]
    [InlineData("date", "not-a-date")]
    [InlineData("state", "not-a-status")]
    [InlineData("enabled", "not-a-bool")]
    async Task should_reject_invalid_values_in_get_and_query(string name, string invalid)
    {
        var performer = Performer();
        foreach (var read in new Func<ModelBoundQueryPerformer, Dictionary<string, string>, Task<QueryArguments>>[] { ReadGet, ReadBody })
        {
            TestReadModel.WasCalled = false;
            var error = await Catch.Exception(() => read(performer, new() { [name] = invalid }));
            var failure = error as IValidationFailure;
            failure.ShouldNotBeNull();
            failure.ValidationResult.Members.ShouldContainOnly(name);
            failure.ValidationResult.Reason.ShouldEqual(ValidationResultReason.MalformedRequest);
            TestReadModel.WasCalled.ShouldBeFalse();
        }
    }

    [Theory]
    [InlineData("required")]
    [InlineData("optional")]
    [InlineData("defaulted")]
    async Task should_reject_invalid_concepts_without_invoking_the_model(string name)
    {
        var performer = ConceptPerformer();
        foreach (var read in new Func<ModelBoundQueryPerformer, Dictionary<string, string>, Task<QueryArguments>>[] { ReadGet, ReadBody })
        {
            ConceptReadModel.WasCalled = false;
            var error = await Catch.Exception(() => read(performer, new() { [name] = "not-a-guid" }));
            (error is InvalidQueryArgument).ShouldBeTrue();
            ((InvalidQueryArgument)error).ValidationResult.Members.ShouldContainOnly(name);
            ((InvalidQueryArgument)error).ValidationResult.Reason.ShouldEqual(ValidationResultReason.MalformedRequest);
            ConceptReadModel.WasCalled.ShouldBeFalse();
        }

        ConceptReadModel.WasCalled = false;
        var directError = await Catch.Exception(async () => await performer.Perform(new QueryContext(
            performer.FullyQualifiedName,
            CorrelationId.New(),
            Paging.NotPaged,
            Sorting.None,
            new QueryArguments { [name] = "not-a-guid" },
            [])));
        (directError is InvalidQueryArgument).ShouldBeTrue();
        ConceptReadModel.WasCalled.ShouldBeFalse();
    }

    [Fact]
    async Task should_bind_valid_concepts_and_leave_omitted_optional_concepts_absent()
    {
        var performer = ConceptPerformer();
        var id = Guid.NewGuid();
        foreach (var read in new Func<ModelBoundQueryPerformer, Dictionary<string, string>, Task<QueryArguments>>[] { ReadGet, ReadBody })
        {
            ConceptReadModel.WasCalled = false;
            var arguments = await read(performer, new() { ["required"] = id.ToString() });
            ((AccountId)arguments["required"]).Value.ShouldEqual(id);
            await performer.Perform(new QueryContext(
                performer.FullyQualifiedName,
                CorrelationId.New(),
                Paging.NotPaged,
                Sorting.None,
                arguments,
                []));
            ConceptReadModel.WasCalled.ShouldBeTrue();
        }
    }

    [Fact]
    async Task should_reject_an_invalid_argument_on_direct_invocation()
    {
        TestReadModel.WasCalled = false;
        var error = await Catch.Exception(() => Perform(Performer(), new QueryArguments { ["count"] = "abc" }));
        (error is InvalidQueryArgument).ShouldBeTrue();
        ((InvalidQueryArgument)error).ValidationResult.Members.ShouldContainOnly("count");
        TestReadModel.WasCalled.ShouldBeFalse();
    }

    [Fact]
    async Task should_bind_valid_values_from_both_transports()
    {
        var performer = Performer();
        var id = Guid.NewGuid();
        var values = new Dictionary<string, string>
        {
            ["count"] = "42", ["id"] = id.ToString(), ["date"] = "2026-05-12", ["state"] = "active", ["enabled"] = "true"
        };
        foreach (var read in new Func<ModelBoundQueryPerformer, Dictionary<string, string>, Task<QueryArguments>>[] { ReadGet, ReadBody })
        {
            TestReadModel.WasCalled = false;
            var model = await Perform(performer, await read(performer, values));
            TestReadModel.WasCalled.ShouldBeTrue();
            model.Count.ShouldEqual(42);
            model.Id.ShouldEqual(id);
            model.Date.ShouldEqual(new DateOnly(2026, 5, 12));
            model.State.ShouldEqual(Status.Active);
            model.Enabled.ShouldBeTrue();
        }
    }
}
