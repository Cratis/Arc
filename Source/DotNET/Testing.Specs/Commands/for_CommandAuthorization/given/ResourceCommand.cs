// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization.given;

[Command]
[Authorize(Policy = "fixture")]
public record ResourceCommand(bool Allow)
{
    public void Handle(PolicyBehavior behavior) => behavior.HandlerCalls++;
    public static void Provide(PolicyBehavior behavior) => behavior.ProvideCalls++;
}
