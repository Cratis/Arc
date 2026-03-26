// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ObservableQueryConnectionPool } from '../../../ObservableQueryConnectionPool';
import { IObservableQueryConnection } from '../../../IObservableQueryConnection';
import { QueryResult } from '../../../QueryResult';

describe('when constructing a pool with zero size', () => {
    let pool: ObservableQueryConnectionPool<string>;
    let connectCalled: boolean;

    beforeEach(() => {
        connectCalled = false;
        const fakeConnection: IObservableQueryConnection<string> = {
            connect: () => { connectCalled = true; },
            disconnect: () => {},
            lastPingLatency: 0,
            averageLatency: 0,
        };

        pool = new ObservableQueryConnectionPool<string>(0, () => fakeConnection);
        pool.acquire((_: QueryResult<string>) => _);
    });

    it('should fall back to at least one slot and still connect', () => connectCalled.should.be.true);
});
