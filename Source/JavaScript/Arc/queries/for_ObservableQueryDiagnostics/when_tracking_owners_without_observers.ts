// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, it } from 'vitest';
import { ObservableQueryDiagnostics, QueryInstanceCache } from '../../queries/index.js';

function createDiagnostics() {
    const cache = new QueryInstanceCache(0);
    let scans = 0;
    const original = cache.getDiagnosticsSnapshot.bind(cache);
    cache.getDiagnosticsSnapshot = () => {
        scans++;
        return original();
    };
    const diagnostics = new ObservableQueryDiagnostics(
        cache,
        () => undefined,
        () => ({ queryTransportMethod: 'ServerSentEvents', queryDirectMode: false })
    );
    return { diagnostics, scans: () => scans };
}

describe('when tracking query owners without observers', () => {
    it('should not scan the cache when beginning and ending tracking', () => {
        const { diagnostics, scans } = createDiagnostics();

        diagnostics.beginTracking('cache-key', 'OrdersPage');
        diagnostics.endTracking('cache-key');

        scans().should.equal(0);
    });

    it('should scan the cache and publish when there is an observer', () => {
        const { diagnostics, scans } = createDiagnostics();
        let published = 0;
        diagnostics.snapshots$.subscribe(() => published++);

        diagnostics.beginTracking('cache-key', 'OrdersPage');
        diagnostics.endTracking('cache-key');

        scans().should.equal(2);
        published.should.equal(2);
    });

    it('should not scan the cache again after the observer unsubscribes', () => {
        const { diagnostics, scans } = createDiagnostics();
        const subscription = diagnostics.snapshots$.subscribe();
        diagnostics.beginTracking('cache-key', 'OrdersPage');
        subscription.unsubscribe();
        const scansBefore = scans();

        diagnostics.endTracking('cache-key');
        diagnostics.beginTracking('other-key', 'OrdersPage');

        scans().should.equal(scansBefore);
    });

    it('should report current ownership from an explicit snapshot after untracked tracking', () => {
        const { diagnostics } = createDiagnostics();

        diagnostics.beginTracking('cache-key', 'OrdersPage');
        diagnostics.beginTracking('other-key', 'OrdersPage');
        diagnostics.endTracking('other-key');

        const ownership = diagnostics.getSnapshot().ownership;
        ownership.ownersByQueryKey.should.deep.equal({ 'cache-key': 'OrdersPage' });
        ownership.queriesByOwner.should.deep.equal({ OrdersPage: ['cache-key'] });
    });
});
