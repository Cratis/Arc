// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace ValidatorRuntimeApplication;

[Command]
public record PlainCommand(string Name)
{
    public void Handle() { }
}
