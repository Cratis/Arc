// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.ProxyGenerator.Specs.RuntimeDependencyApplication;

[Command]
public class CreateApplication
{
    public string Name { get; set; } = string.Empty;

    public void Handle() { }
}
