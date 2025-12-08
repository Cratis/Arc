// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;

namespace TestConsole;

public class TestIdentityProvider : IProvideIdentityDetails
{
    public Task<IdentityDetails> Provide(IdentityProviderContext context)
    {
        var result = new IdentityDetails(
            true,
            new
            {
                Message = "Test Console User",
                Context = new
                {
                    context.Id,
                    context.Name
                }
            });

        return Task.FromResult(result);
    }
}
