// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values.given;

[Command]
public record AllocateNestedCartId
{
    public async Task<object> Handle([GeneratedValue("cart")] CartId cartId, ICommandPipeline pipeline)
    {
        var child = await pipeline.Execute<CartId>(new AllocateCartId());

        return new CartId[] { cartId, child.Response! };
    }
}
