// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryHubConnection } from '../../IObservableQueryHubConnection';
import { getOrCreateMultiplexer, resetSharedMultiplexer } from '../../ObservableQueryMultiplexer';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when getting or creating with a changed cache key', () => {
    let factoryCallCount: number;
    let disposedConnectionCount: number;

    beforeEach(() => {
        factoryCallCount = 0;
        disposedConnectionCount = 0;
        const factory = (): IObservableQueryHubConnection => {
            factoryCallCount++;
            return {
                queryCount: 0,
                lastPingLatency: 0,
                averageLatency: 0,
                subscribe: () => {},
                unsubscribe: () => {},
                dispose: () => { disposedConnectionCount++; },
            };
        };
        getOrCreateMultiplexer(factory, 'key-one', 1);
        getOrCreateMultiplexer(factory, 'key-two', 1);
    });

    afterEach(() => {
        resetSharedMultiplexer();
    });

    it('should dispose the connections of the previous multiplexer', () => {
        disposedConnectionCount.should.be.greaterThan(0);
    });

    it('should create connections for both multiplexers', () => {
        factoryCallCount.should.equal(2);
    });
});
