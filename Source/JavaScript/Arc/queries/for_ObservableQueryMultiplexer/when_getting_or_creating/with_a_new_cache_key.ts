// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryHubConnection } from '../../IObservableQueryHubConnection';
import { getOrCreateMultiplexer, ObservableQueryMultiplexer, resetSharedMultiplexer } from '../../ObservableQueryMultiplexer';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when getting or creating with a new cache key', () => {
    let multiplexer: ObservableQueryMultiplexer;
    let factoryCallCount: number;

    beforeEach(() => {
        factoryCallCount = 0;
        const factory = (): IObservableQueryHubConnection => {
            factoryCallCount++;
            return { queryCount: 0, lastPingLatency: 0, averageLatency: 0, subscribe: () => {}, unsubscribe: () => {}, dispose: () => {} };
        };
        multiplexer = getOrCreateMultiplexer(factory, 'test-key', 1);
    });

    afterEach(() => {
        resetSharedMultiplexer();
    });

    it('should return a multiplexer', () => {
        multiplexer.should.be.instanceOf(ObservableQueryMultiplexer);
    });

    it('should call the factory to create connections', () => {
        factoryCallCount.should.be.greaterThan(0);
    });
});

