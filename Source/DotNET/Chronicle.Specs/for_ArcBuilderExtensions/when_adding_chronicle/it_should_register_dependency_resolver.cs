// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.for_ArcBuilderExtensions.when_adding_chronicle;

public class it_should_register_dependency_resolver : given.an_arc_builder
{
    void Because() => _builder.WithChronicle();

    [Fact] void should_register_read_model_dependency_resolver() =>
        _services.ShouldContain(_ => _.ServiceType == typeof(ICommandDependencyResolver) && _.ImplementationType == typeof(ReadModelDependencyResolver));
}
