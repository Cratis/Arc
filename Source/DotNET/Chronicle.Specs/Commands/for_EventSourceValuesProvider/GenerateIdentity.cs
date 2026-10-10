// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Chronicle.Commands.for_EventSourceValuesProvider;

[Command]
public record GenerateIdentity
{
    public IdentityGenerated Handle(IIdentitySource identities) => new(identities.NewGuid());
}
