// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadModelServiceCollectionExtensions;

public class when_adding_command_aware_decision_reads : Specification
{
    ServiceProvider _services;
    ProtectedReadModelKeyTraversal? _traversal;

    void Establish() => _services = new ServiceCollection().AddLogging().AddCommandAwareDecisionReads().BuildServiceProvider();

    void Because() => _traversal = ProtectedReadModelKeys.Traversal(_services);

    [Fact] void should_provide_the_protected_key_traversal() => _traversal.ShouldNotBeNull();
    [Fact] void should_share_one_traversal() => _services.GetRequiredService<ProtectedReadModelKeyTraversal>().ShouldBeSame(_traversal);

    void Destroy() => _services.Dispose();
}
