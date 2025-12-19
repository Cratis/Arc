// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_connection } from './given/an_observable_query_connection';
import { given } from '../../given';

describe('when constructing', given(an_observable_query_connection, context => {
    it('should have zero last ping latency', () => {
        context.connection.lastPingLatency.should.equal(0);
    });

    it('should have zero average latency', () => {
        context.connection.averageLatency.should.equal(0);
    });
}));
