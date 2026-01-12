// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_CommandExtensions.TestTypes.Products;

[Command]
public class CreateProduct
{
    public string ProductId { get; set; } = string.Empty;
    public void Handle() { }
}