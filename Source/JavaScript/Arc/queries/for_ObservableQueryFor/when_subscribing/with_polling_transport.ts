// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { given } from '../../../given.js';
import { Globals } from '../../../Globals.js';
import { createFetchHelper } from '../../../helpers/fetchHelper.js';
import { an_observable_query_for } from '../given/an_observable_query_for.js';
import { QueryTransportMethod } from '../../QueryTransportMethod.js';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription.js';
import { Paging } from '../../Paging.js';
import { Sorting } from '../../Sorting.js';
import { SortDirection } from '../../SortDirection.js';

describe('when subscribing with polling transport', given(an_observable_query_for, context => {
    const fetchHelper = createFetchHelper();
    let originals: Pick<typeof Globals, 'queryDirectMode' | 'queryTransportMethod' | 'queryPollingOptions'>;
    let subscription: ObservableQuerySubscription<string>;
    let clock: sinon.SinonFakeTimers;
    let fetchStub: sinon.SinonStub;
    let callback: sinon.SinonStub;
    let callsBeforeInterval: number;
    let callsAtInterval: number;

    beforeEach(async () => {
        originals = { queryDirectMode: Globals.queryDirectMode, queryTransportMethod: Globals.queryTransportMethod, queryPollingOptions: Globals.queryPollingOptions };
        Globals.queryDirectMode = false;
        Globals.queryTransportMethod = QueryTransportMethod.Polling;
        Globals.queryPollingOptions = { ...Globals.queryPollingOptions, minIntervalMs: 100 };
        // Only setTimeout/clearTimeout/Date need faking here. Faking queueMicrotask/nextTick/setImmediate
        // breaks Node's native fetch (undici), whose body-stream consumption relies on them internally.
        clock = sinon.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'Date'] });
        fetchStub = fetchHelper.stubFetch();
        fetchStub.callsFake(async () => new Response(JSON.stringify({
            data: 'value', isSuccess: true, isAuthorized: true, isValid: true,
            hasExceptions: false, validationResults: [], exceptionMessages: [], exceptionStackTrace: '',
            paging: { page: 2, size: 10, totalItems: 35, totalPages: 4 }
        })));
        callback = sinon.stub();
        const query = context.queryWithRouteAndQueryArgs;
        query.setOrigin('https://example.com');
        query.setMicroservice('svc');
        query.setHttpHeadersCallback(() => ({ Authorization: 'Bearer test' }));
        query.setPollingOptions({ intervalMs: 250 });
        query.paging = new Paging(2, 10);
        query.sorting = new Sorting('name', SortDirection.descending);
        subscription = query.subscribe(callback, { id: 'item-42', filter: 'active', limit: 50 });
        await clock.tickAsync(0);
        await clock.tickAsync(249);
        callsBeforeInterval = fetchStub.callCount;
        await clock.tickAsync(1);
        callsAtInterval = fetchStub.callCount;
        subscription.unsubscribe();
        await clock.tickAsync(2000);
    });
    afterEach(() => {
        subscription?.unsubscribe();
        clock.restore();
        fetchHelper.restore();
        Object.assign(Globals, originals);
    });
    it('should embed route arguments and append only HTTP query arguments', () => {
        const url = new URL(fetchStub.firstCall.args[0]);
        url.pathname.should.equal('/api/items/item-42');
        Object.fromEntries(url.searchParams).should.deep.equal({ filter: 'active', limit: '50', page: '2', pageSize: '10', sortBy: 'name', sortDirection: 'desc', waitForFirstResult: 'true' });
    });
    it('should use per-query headers', () => fetchStub.firstCall.args[1].headers.get('Authorization').should.equal('Bearer test'));
    it('should deliver a ready deserialized result with paging metadata', () => {
        callback.firstCall.args[0].data.should.equal('value');
        callback.firstCall.args[0].isReady.should.be.true;
        callback.firstCall.args[0].paging.totalItems.should.equal(35);
    });
    it('should apply the per-query polling interval', () => {
        callsBeforeInterval.should.equal(1);
        callsAtInterval.should.equal(2);
    });
    it('should suppress unchanged snapshots after deserialization', () => callback.callCount.should.equal(1));
    it('should stop after unsubscribe', () => fetchStub.callCount.should.equal(callsAtInterval));
}));

describe('when subscribing with polling transport and invalid arguments', given(an_observable_query_for, context => {
    const fetchHelper = createFetchHelper();
    let originalTransport: QueryTransportMethod;
    let subscription: ObservableQuerySubscription<string>;
    let clock: sinon.SinonFakeTimers;
    let fetchStub: sinon.SinonStub;
    let callback: sinon.SinonStub;
    beforeEach(async () => {
        originalTransport = Globals.queryTransportMethod;
        Globals.queryTransportMethod = QueryTransportMethod.Polling;
        // Only setTimeout/clearTimeout/Date need faking here. Faking queueMicrotask/nextTick/setImmediate
        // breaks Node's native fetch (undici), whose body-stream consumption relies on them internally.
        clock = sinon.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'Date'] });
        fetchStub = fetchHelper.stubFetch();
        fetchStub.callsFake(async () => new Response(JSON.stringify({
            data: '', isSuccess: false, isAuthorized: true, isValid: false,
            hasExceptions: false, validationResults: [{ message: 'Invalid filter' }],
            exceptionMessages: [], exceptionStackTrace: '',
            paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
        }), { status: 400 }));
        callback = sinon.stub();
        context.queryWithRouteAndQueryArgs.setOrigin('https://example.com');
        subscription = context.queryWithRouteAndQueryArgs.subscribe(callback, { id: 'item', filter: 'invalid', limit: 10 });
        await clock.tickAsync(60000);
    });
    afterEach(() => {
        subscription?.unsubscribe();
        clock.restore();
        fetchHelper.restore();
        Globals.queryTransportMethod = originalTransport;
    });
    it('should deliver ready validation results', () => {
        callback.firstCall.args[0].isReady.should.be.true;
        callback.firstCall.args[0].isValid.should.be.false;
        callback.firstCall.args[0].validationResults[0].message.should.equal('Invalid filter');
    });
    it('should stop polling', () => fetchStub.callCount.should.equal(1));
}));
