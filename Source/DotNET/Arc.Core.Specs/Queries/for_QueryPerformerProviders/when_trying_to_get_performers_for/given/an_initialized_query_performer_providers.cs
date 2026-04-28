// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryPerformerProviders.when_trying_to_get_performers_for.given;

public class an_initialized_query_performer_providers : for_QueryPerformerProviders.given.two_query_performers
{
    void Establish() => _queryPerformerProviders = new(new KnownInstancesOf<IQueryPerformerProvider>([_firstProvider, _secondProvider]));
}