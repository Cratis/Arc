// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { Globals } from '../../../Globals.js';
import { createFetchHelper } from '../../../helpers/fetchHelper.js';
import { PollingQueryConnection } from '../../PollingQueryConnection.js';

export class a_polling_query_connection {
    connection!: PollingQueryConnection<unknown>;
    dataReceived!: sinon.SinonStub;
    fetchStub!: sinon.SinonStub;
    clock!: sinon.SinonFakeTimers;
    random!: sinon.SinonStub;
    responses: Response[] = [];
    private _correlationId = 0;

    constructor() {
        const fetchHelper = createFetchHelper();
        const originalOptions = Globals.queryPollingOptions;
        beforeEach(() => {
            // Only setTimeout/clearTimeout/Date need faking here. Faking queueMicrotask/nextTick/setImmediate
            // breaks Node's native fetch (undici), whose body-stream consumption relies on them internally.
            this.clock = sinon.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'Date'] });
            this.random = sinon.stub(Math, 'random').returns(0.5);
            sinon.stub(console, 'warn');
            this.responses = [
                new Response(JSON.stringify(this.envelope('same'))),
                new Response(JSON.stringify(this.envelope('same')))
            ];
            this.fetchStub = fetchHelper.stubFetch();
            this.fetchStub.callsFake(async () => this.responses.shift() ?? new Response(JSON.stringify(this.envelope('same'))));
            this.dataReceived = sinon.stub();
            this.connection = new PollingQueryConnection(new URL('https://example.com/api/items/42'),
                () => ({ Authorization: 'Bearer x' }), 'svc', {
                    intervalMs: 1000, minIntervalMs: 500, maxIntervalMs: 8000,
                    backoffMultiplier: 2, jitterRatio: 0.2
                });
        });
        afterEach(() => {
            this.connection.disconnect();
            this.clock.restore();
            fetchHelper.restore();
            sinon.restore();
            Globals.queryPollingOptions = originalOptions;
        });
    }

    envelope(data: unknown, overrides: Record<string, unknown> = {}) {
        return {
            data, correlationId: String(++this._correlationId), isSuccess: true,
            isAuthorized: true, isValid: true, hasExceptions: false,
            validationResults: [], exceptionMessages: [], exceptionStackTrace: '',
            paging: { page: 1, size: 10, totalItems: 25, totalPages: 3 }, ...overrides
        };
    }

    respond(status: number, headers: Record<string, string> = {}, overrides: Record<string, unknown> = {}) {
        this.fetchStub.onFirstCall().resolves(new Response(JSON.stringify(this.envelope('same', overrides)), { status, headers }));
    }
}
