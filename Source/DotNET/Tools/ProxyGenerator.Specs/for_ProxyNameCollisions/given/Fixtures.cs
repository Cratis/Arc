// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ProxyNameCollisions.Fixtures;

public record FirstReadModel;

public record SecondReadModel;

public record FakeDescriptor(Type Type, string Name) : IDescriptor
{
    public IEnumerable<Type> TypesInvolved => [];
}
