// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { ObservableQueryDiagnostics, QueryInstanceCache } from '../../queries';

describe('when tracking query owners', () => {
    it('should publish diagnostics snapshots through an observable', () => {
        const diagnostics = new ObservableQueryDiagnostics(
            new QueryInstanceCache(0),
            () => undefined,
            () => ({ queryTransportMethod: 'ServerSentEvents', queryDirectMode: false })
        );

        const snapshots: string[] = [];
        diagnostics.snapshots$.subscribe(snapshot => snapshots.push(JSON.stringify(snapshot.ownership)));

        diagnostics.beginTracking('cache-key', 'OrdersPage');
        snapshots.should.have.lengthOf(1);
        snapshots[0].should.contain('cache-key');
        snapshots[0].should.contain('OrdersPage');

        diagnostics.endTracking('cache-key');
        snapshots.should.have.lengthOf(2);
        snapshots[1].should.equal('{"ownersByQueryKey":{},"queriesByOwner":{}}');
    });

    it('should not dispatch browser events when publishing snapshots', () => {
        const diagnostics = new ObservableQueryDiagnostics(
            new QueryInstanceCache(0),
            () => undefined,
            () => ({ queryTransportMethod: 'ServerSentEvents', queryDirectMode: false })
        );

        const globalWithWindow = globalThis as Record<string, unknown>;
        const originalWindow = globalWithWindow.window;
        const dispatchEvent = function(): never {
            throw new Error('dispatchEvent should not be called');
        };

        try {
            globalWithWindow.window = { dispatchEvent };
            diagnostics.beginTracking('cache-key', 'OrdersPage');
            diagnostics.endTracking('cache-key');
        } finally {
            globalWithWindow.window = originalWindow;
        }
    });
});