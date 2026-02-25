// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryPerformer.when_checking_is_enumerable_result;

#pragma warning disable CA1822
public static class OrderQueries
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
