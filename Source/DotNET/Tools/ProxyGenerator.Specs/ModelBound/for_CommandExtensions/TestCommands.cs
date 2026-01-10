// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_CommandExtensions.TestTypes.Orders;

[Command]
public class CreateOrder
{
    public string OrderId { get; set; } = string.Empty;
    public void Handle() { }
}

[Command]
public class UpdateOrder
{
    public string OrderId { get; set; } = string.Empty;
    public void Handle() { }
}
