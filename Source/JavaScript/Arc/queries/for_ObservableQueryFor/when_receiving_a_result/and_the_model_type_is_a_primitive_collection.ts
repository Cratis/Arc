// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as sinon from 'sinon';
import { an_observable_query_for } from '../given/an_observable_query_for';
import { given } from '../../../given';
import { Globals } from '../../../Globals';
import { EventSourceFactory } from '../../../EventSourceFactory';
import { QueryResult } from '../../QueryResult';
import { QueryTransportMethod } from '../../QueryTransportMethod';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription';

interface FakeEventSource {
    onmessage: ((event: MessageEvent) => void) | null;
    onerror: (() => void) | null;
    close: sinon.SinonStub;
}

describe('and the model type is a primitive collection', given(an_observable_query_for, context => {
    let subscription: ObservableQuerySubscription<string[]>;
    let fakeEventSource: FakeEventSource;
    let received: QueryResult<string[]>[];
    let originalQueryDirectMode: boolean;
    let originalTransportMethod: QueryTransportMethod;
    let originalFactory: EventSourceFactory | undefined;

    beforeEach(() => {
        originalQueryDirectMode = Globals.queryDirectMode;
        originalTransportMethod = Globals.queryTransportMethod;
        originalFactory = Globals.eventSourceFactory;

        Globals.queryDirectMode = true;
        Globals.queryTransportMethod = QueryTransportMethod.ServerSentEvents;

        fakeEventSource = { onmessage: null, onerror: null, close: sinon.stub() };
        Globals.eventSourceFactory = (() => fakeEventSource) as unknown as EventSourceFactory;

        context.enumerableQuery.setOrigin('https://example.com');

        received = [];
        subscription = context.enumerableQuery.subscribe(
            result => received.push(result),
            { category: 'test-category' });

        fakeEventSource.onmessage!({
            data: JSON.stringify({
                data: ['first', 'second'],
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
            })
        } as MessageEvent);
    });

    afterEach(() => {
        subscription?.unsubscribe();
        Globals.queryDirectMode = originalQueryDirectMode;
        Globals.queryTransportMethod = originalTransportMethod;
        Globals.eventSourceFactory = originalFactory;
        sinon.restore();
    });

    it('should deliver the result to the subscriber', () => received.length.should.equal(1));
    it('should keep every item a primitive string', () => received[0].data.every(_ => typeof _ === 'string').should.be.true);
    it('should keep the values intact', () => received[0].data.should.deep.equal(['first', 'second']));
}));
