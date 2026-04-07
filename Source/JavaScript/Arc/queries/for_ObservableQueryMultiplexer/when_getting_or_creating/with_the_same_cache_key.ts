// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryHubConnection } from '../../IObservableQueryHubConnection';
import { getOrCreateMultiplexer, ObservableQueryMultiplexer, resetSharedMultiplexer } from '../../ObservableQueryMultiplexer';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when getting or creating with the same cache key', () => {
    let first: ObservableQueryMultiplexer;
    let second: ObservableQueryMultiplexer;
    let factoryCallCount: number;

    beforeEach(() => {
        factoryCallCount = 0;
        const factory = (): IObservableQueryHubConnection => {
            factoryCallCount++;
            return { queryCount: 0, lastPingLatency: 0, averageLatency: 0, subscribe: () => {}, unsubscribe: () => {}, dispose: () => {} };
        };
        first = getOrCreateMultiplexer(factory, 'test-key', 1);
        second = getOrCreateMultiplexer(factory, 'test-key', 1);
    });

    afterEach(() => {
        resetSharedMultiplexer();
    });

    it('should return the same instance', () => {
        second.should.equal(first);
    });

    it('should only call the factory once', () => {
        factoryCallCount.should.equal(1);
    });
});
