// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.ControllerBased.for_QueryPerformerProvider;

public class when_checking_controller_query_allows_anonymous_access : given.a_controller_query_performer_provider
{
    IQueryPerformer _anonymousPerformer;
    IQueryPerformer _securedPerformer;

    void Establish()
    {
        // Get the performers by finding them in the provider
        var performers = _provider.Performers.ToList();
        
        _anonymousPerformer = performers.FirstOrDefault(p => p.Name == nameof(given.TestController.AnonymousEventStores))!;
        _securedPerformer = performers.FirstOrDefault(p => p.Name == nameof(given.TestController.AllEventStores))!;
    }

    [Fact] void should_find_anonymous_performer() => _anonymousPerformer.ShouldNotBeNull();
    [Fact] void should_find_secured_performer() => _securedPerformer.ShouldNotBeNull();
    [Fact] void should_allow_anonymous_access_for_method_with_allow_anonymous_attribute() => _anonymousPerformer.AllowsAnonymousAccess.ShouldBeTrue();
    [Fact] void should_not_allow_anonymous_access_for_method_without_allow_anonymous_attribute() => _securedPerformer.AllowsAnonymousAccess.ShouldBeFalse();
}
