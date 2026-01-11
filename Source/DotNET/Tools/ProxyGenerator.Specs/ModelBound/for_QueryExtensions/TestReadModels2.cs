// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_QueryExtensions.TestTypes.Orders;

public class Order
{
    public string OrderId { get; set; } = string.Empty;

    public static Order GetById(string id) => new() { OrderId = id };
    public static IEnumerable<Order> GetAll() => [];
}