// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { ObservableQueryMultiplexer } from '../../ObservableQueryMultiplexer.js';
import { SubscriptionRequest } from '../../WebSocketHubConnection.js';

const parts = { quoted: '"abc"', number: '42', comma: 'a,b' };

describe('when building a subscription request with dictionary values', () => {
    it('should send the dictionary as one JSON string argument', () => {
        let received: SubscriptionRequest | undefined;
        const multiplexer = new ObservableQueryMultiplexer(1, () => ({
            queryCount: 0, lastPingLatency: 0, averageLatency: 0, isConnected: false,
            subscribe: (_id, request) => { received = request; },
            unsubscribe: () => {}, dispose: () => {}
        }));
        const date = new Date('2026-10-24T10:00:00Z');
        const unsubscribe = multiplexer.subscribe('Parts.Get', { parts, count: 42, names: ['a', 'b'], date }, () => {});
        received!.arguments!.parts!.should.equal(JSON.stringify(parts));
        received!.arguments!.count!.should.equal('42');
        received!.arguments!.names!.should.equal('a,b');
        received!.arguments!.date!.should.equal(String(date));
        unsubscribe();
        multiplexer.dispose();
    });
});
