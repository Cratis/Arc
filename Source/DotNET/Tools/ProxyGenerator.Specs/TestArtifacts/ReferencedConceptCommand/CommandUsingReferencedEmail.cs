// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

namespace Cratis.Arc.ProxyGenerator.Specs.ReferencedConceptCommand;

[Command]
public class CommandUsingReferencedEmail
{
    public ReferencedEmail Email { get; set; } = new(string.Empty);

    public void Handle()
    {
    }
}
