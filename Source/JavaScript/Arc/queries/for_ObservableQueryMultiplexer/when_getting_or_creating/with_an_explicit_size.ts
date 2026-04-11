// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryHubConnection } from '../../IObservableQueryHubConnection';
import { getOrCreateMultiplexer, ObservableQueryMultiplexer, resetSharedMultiplexer } from '../../ObservableQueryMultiplexer';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when getting or creating with an explicit size', () => {
    let multiplexer: ObservableQueryMultiplexer;
    let factoryCallCount: number;

    beforeEach(() => {
        factoryCallCount = 0;
        const factory = (): IObservableQueryHubConnection => {
            factoryCallCount++;
            return { queryCount: 0, lastPingLatency: 0, averageLatency: 0, subscribe: () => {}, unsubscribe: () => {}, dispose: () => {} };
        };
        multiplexer = getOrCreateMultiplexer(factory, 'test-key', 3);
    });

    afterEach(() => {
        resetSharedMultiplexer();
    });

    it('should create a multiplexer with the given size', () => {
        multiplexer.size.should.equal(3);
    });

    it('should create exactly that many connections', () => {
        factoryCallCount.should.equal(3);
    });
});
