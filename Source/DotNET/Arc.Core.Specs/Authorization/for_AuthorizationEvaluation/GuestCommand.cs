// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluation;

[Command]
[Authorize(Policy = "GuestPermission")]
public record GuestCommand
{
    static int _handled;
    public static int Handled => Volatile.Read(ref _handled);
    public void Handle() => Interlocked.Increment(ref _handled);
}
