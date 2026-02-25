// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_checking_is_enumerable_result;

public record OrderReadModel(string Id, string Name);

#pragma warning disable CA1822
public class OrderQueries
{
    public static IEnumerable<OrderReadModel> AllOrders() => [];
    public static Task<IEnumerable<OrderReadModel>> AllOrdersAsync() => Task.FromResult<IEnumerable<OrderReadModel>>([]);
    public static OrderReadModel[] AllOrdersArray() => [];
    public static List<OrderReadModel> AllOrdersList() => [];
    public static IAsyncEnumerable<OrderReadModel> AllOrdersAsyncEnumerable() => AsyncEnumerable.Empty<OrderReadModel>();
    public static ISubject<IEnumerable<OrderReadModel>> ObservableOrders() => new ReplaySubject<IEnumerable<OrderReadModel>>();
    public static OrderReadModel SingleOrder() => new("1", "Test");
    public static Task<OrderReadModel> SingleOrderAsync() => Task.FromResult(new OrderReadModel("1", "Test"));
    public static ISubject<OrderReadModel> ObservableSingleOrder() => new ReplaySubject<OrderReadModel>();
}
#pragma warning restore CA1822

public class with_ienumerable_return_type : Specification
{
    ModelBoundQueryPerformer _performer;

    void Establish()
    {
        var serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        var authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        var method = typeof(OrderQueries).GetMethod(nameof(OrderQueries.AllOrders))!;
        _performer = new ModelBoundQueryPerformer(typeof(OrderReadModel), method, serviceProviderIsService, authorizationEvaluator);
    }

    [Fact] void should_be_enumerable_result() => _performer.IsEnumerableResult.ShouldBeTrue();
}

public class with_task_of_ienumerable_return_type : Specification
{
    ModelBoundQueryPerformer _performer;

    void Establish()
    {
        var serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        var authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        var method = typeof(OrderQueries).GetMethod(nameof(OrderQueries.AllOrdersAsync))!;
        _performer = new ModelBoundQueryPerformer(typeof(OrderReadModel), method, serviceProviderIsService, authorizationEvaluator);
    }

    [Fact] void should_be_enumerable_result() => _performer.IsEnumerableResult.ShouldBeTrue();
}

public class with_array_return_type : Specification
{
    ModelBoundQueryPerformer _performer;

    void Establish()
    {
        var serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        var authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        var method = typeof(OrderQueries).GetMethod(nameof(OrderQueries.AllOrdersArray))!;
        _performer = new ModelBoundQueryPerformer(typeof(OrderReadModel), method, serviceProviderIsService, authorizationEvaluator);
    }

    [Fact] void should_be_enumerable_result() => _performer.IsEnumerableResult.ShouldBeTrue();
}

public class with_list_return_type : Specification
{
    ModelBoundQueryPerformer _performer;

    void Establish()
    {
        var serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        var authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        var method = typeof(OrderQueries).GetMethod(nameof(OrderQueries.AllOrdersList))!;
        _performer = new ModelBoundQueryPerformer(typeof(OrderReadModel), method, serviceProviderIsService, authorizationEvaluator);
    }

    [Fact] void should_be_enumerable_result() => _performer.IsEnumerableResult.ShouldBeTrue();
}

public class with_async_enumerable_return_type : Specification
{
    ModelBoundQueryPerformer _performer;

    void Establish()
    {
        var serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        var authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        var method = typeof(OrderQueries).GetMethod(nameof(OrderQueries.AllOrdersAsyncEnumerable))!;
        _performer = new ModelBoundQueryPerformer(typeof(OrderReadModel), method, serviceProviderIsService, authorizationEvaluator);
    }

    [Fact] void should_be_enumerable_result() => _performer.IsEnumerableResult.ShouldBeTrue();
}

public class with_subject_of_ienumerable_return_type : Specification
{
    ModelBoundQueryPerformer _performer;

    void Establish()
    {
        var serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        var authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        var method = typeof(OrderQueries).GetMethod(nameof(OrderQueries.ObservableOrders))!;
        _performer = new ModelBoundQueryPerformer(typeof(OrderReadModel), method, serviceProviderIsService, authorizationEvaluator);
    }

    // ISubject<IEnumerable<T>> is a streaming/observable result handled via WebSocket, not pageable
    [Fact] void should_not_be_enumerable_result() => _performer.IsEnumerableResult.ShouldBeFalse();
}

public class with_single_return_type : Specification
{
    ModelBoundQueryPerformer _performer;

    void Establish()
    {
        var serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        var authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        var method = typeof(OrderQueries).GetMethod(nameof(OrderQueries.SingleOrder))!;
        _performer = new ModelBoundQueryPerformer(typeof(OrderReadModel), method, serviceProviderIsService, authorizationEvaluator);
    }

    [Fact] void should_not_be_enumerable_result() => _performer.IsEnumerableResult.ShouldBeFalse();
}

public class with_task_of_single_return_type : Specification
{
    ModelBoundQueryPerformer _performer;

    void Establish()
    {
        var serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        var authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        var method = typeof(OrderQueries).GetMethod(nameof(OrderQueries.SingleOrderAsync))!;
        _performer = new ModelBoundQueryPerformer(typeof(OrderReadModel), method, serviceProviderIsService, authorizationEvaluator);
    }

    [Fact] void should_not_be_enumerable_result() => _performer.IsEnumerableResult.ShouldBeFalse();
}

public class with_subject_of_single_return_type : Specification
{
    ModelBoundQueryPerformer _performer;

    void Establish()
    {
        var serviceProviderIsService = Substitute.For<IServiceProviderIsService>();
        var authorizationEvaluator = Substitute.For<IAuthorizationEvaluator>();
        var method = typeof(OrderQueries).GetMethod(nameof(OrderQueries.ObservableSingleOrder))!;
        _performer = new ModelBoundQueryPerformer(typeof(OrderReadModel), method, serviceProviderIsService, authorizationEvaluator);
    }

    [Fact] void should_not_be_enumerable_result() => _performer.IsEnumerableResult.ShouldBeFalse();
}
