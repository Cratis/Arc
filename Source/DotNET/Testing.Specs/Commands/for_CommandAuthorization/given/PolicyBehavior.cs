// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization.given;

public class PolicyBehavior
{
    public Func<AuthorizationPolicyContext, CancellationToken, Task<bool>> Evaluate { get; set; } = async (context, token) =>
    {
        await Task.Yield();
        token.ThrowIfCancellationRequested();
        return ((CommandContext)context.Resource).Command is ResourceCommand { Allow: true };
    };

    public int PolicyCalls { get; set; }
    public int HandlerCalls { get; set; }
    public int ValidatorCalls { get; set; }
    public int ProvideCalls { get; set; }
}
