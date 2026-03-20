// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { ObservableQueryConnectionPool } from '../../../../ObservableQueryConnectionPool';
import { IObservableQueryConnection } from '../../../../IObservableQueryConnection';
import { DataReceived } from '../../../../ObservableQueryConnection';
import { QueryResult } from '../../../../QueryResult';

describe('when acquiring from a pool with a single slot', () => {
    let connectStub: sinon.SinonStub;
    let disconnectStub: sinon.SinonStub;
    let pool: ObservableQueryConnectionPool<string>;
    let capturedCallback: DataReceived<string> | undefined;
    let cleanup: () => void;

    beforeEach(() => {
        connectStub = sinon.stub().callsFake((cb: DataReceived<string>) => { capturedCallback = cb; });
        disconnectStub = sinon.stub();

        const fakeConnection: IObservableQueryConnection<string> = {
            connect: connectStub,
            disconnect: disconnectStub,
            lastPingLatency: 0,
            averageLatency: 0,
        };

        pool = new ObservableQueryConnectionPool<string>(1, () => fakeConnection);
        cleanup = pool.acquire((result: QueryResult<string>) => result);
    });

    afterEach(() => {
        sinon.restore();
    });

    it('should call connect on the underlying connection', () => connectStub.calledOnce.should.be.true);
    it('should provide a cleanup function', () => (typeof cleanup).should.equal('function'));

    describe('when the cleanup function is invoked', () => {
        beforeEach(() => {
            cleanup();
        });

        it('should disconnect the connection', () => disconnectStub.calledOnce.should.be.true);
    });
});
