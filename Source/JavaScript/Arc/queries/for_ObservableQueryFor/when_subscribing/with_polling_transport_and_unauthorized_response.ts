// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { given } from '../../../given.js';
import { Globals } from '../../../Globals.js';
import { createFetchHelper } from '../../../helpers/fetchHelper.js';
import { an_observable_query_for } from '../given/an_observable_query_for.js';
import { QueryTransportMethod } from '../../QueryTransportMethod.js';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription.js';

describe('when subscribing with polling transport and unauthorized response', given(an_observable_query_for, context => {
    const fetchHelper = createFetchHelper();
    let originalTransport: QueryTransportMethod;
    let clock: sinon.SinonFakeTimers;
    let fetchStub: sinon.SinonStub;
    let callback: sinon.SinonStub;
    let subscription: ObservableQuerySubscription<string>;
    beforeEach(async () => {
        originalTransport = Globals.queryTransportMethod;
        Globals.queryTransportMethod = QueryTransportMethod.Polling;
        clock = sinon.useFakeTimers();
        sinon.stub(console, 'warn');
        fetchStub = fetchHelper.stubFetch();
        fetchStub.callsFake(async () => new Response('Unauthorized', { status: 401 }));
        callback = sinon.stub();
        context.queryWithRouteAndQueryArgs.setOrigin('https://example.com');
        subscription = context.queryWithRouteAndQueryArgs.subscribe(callback, { id: 'item', filter: 'active', limit: 10 });
        await clock.tickAsync(60000);
    });
    afterEach(() => {
        subscription?.unsubscribe();
        clock.restore();
        fetchHelper.restore();
        sinon.restore();
        Globals.queryTransportMethod = originalTransport;
    });
    it('should deliver a ready unauthorized result', () => {
        callback.firstCall.args[0].isAuthorized.should.be.false;
        callback.firstCall.args[0].isReady.should.be.true;
    });
    it('should stop polling', () => fetchStub.callCount.should.equal(1));
}));
