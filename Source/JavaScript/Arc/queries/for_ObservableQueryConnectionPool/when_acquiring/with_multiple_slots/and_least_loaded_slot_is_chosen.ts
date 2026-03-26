// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ObservableQueryConnectionPool } from '../../../ObservableQueryConnectionPool';
import { IObservableQueryConnection } from '../../../IObservableQueryConnection';
import { QueryResult } from '../../../QueryResult';

describe('when acquiring from a pool with multiple slots and the least-loaded slot is chosen', () => {
    let connectStubs: sinon.SinonStub[];
    let disconnectStubs: sinon.SinonStub[];
    let pool: ObservableQueryConnectionPool<string>;
    let cleanups: Array<() => void>;

    beforeEach(() => {
        connectStubs = [];
        disconnectStubs = [];
        cleanups = [];

        pool = new ObservableQueryConnectionPool<string>(3, () => {
            const connectStub = sinon.stub();
            const disconnectStub = sinon.stub();
            connectStubs.push(connectStub);
            disconnectStubs.push(disconnectStub);
            return {
                connect: connectStub,
                disconnect: disconnectStub,
                lastPingLatency: 0,
                averageLatency: 0,
            } as IObservableQueryConnection<string>;
        });

        // First subscriber fills slot 0
        cleanups.push(pool.acquire((_: QueryResult<string>) => _));
        // Release slot 0 — now it is the least-loaded again
        cleanups[0]();
        // Next subscriber should re-use slot 0 (count = 0) not slot 1 (never used but same count)
        cleanups.push(pool.acquire((_: QueryResult<string>) => _));
    });

    afterEach(() => {
        sinon.restore();
    });

    it('should have created two connections', () => connectStubs.length.should.equal(2));
    it('should have called connect on both', () => connectStubs.every(s => s.calledOnce).should.be.true);
});
