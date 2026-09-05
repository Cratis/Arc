// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import * as sinon from 'sinon';
import { DateOnly, TimeOnly } from '@cratis/fundamentals';
import { an_observable_query_for } from '../given/an_observable_query_for';
import type { TestCalendarItem } from '../given/TestQueries';
import { given } from '../../../given';
import { Globals } from '../../../Globals';
import type { EventSourceFactory } from '../../../EventSourceFactory';
import type { QueryResult } from '../../QueryResult';
import { QueryTransportMethod } from '../../QueryTransportMethod';
import type { ObservableQuerySubscription } from '../../ObservableQuerySubscription';

interface FakeEventSource {
    onmessage: ((event: MessageEvent) => void) | null;
    onerror: (() => void) | null;
    close: sinon.SinonStub;
}

describe('and the model has a calendar collection', given(an_observable_query_for, context => {
    let subscription: ObservableQuerySubscription<TestCalendarItem[]>;
    let fakeEventSource: FakeEventSource;
    let received: QueryResult<TestCalendarItem[]>[];
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
        // SAFETY: The test double implements the EventSource members exercised by the query connection.
        Globals.eventSourceFactory = (() => fakeEventSource) as unknown as EventSourceFactory;

        context.calendarEnumerableQuery.setOrigin('https://example.com');

        received = [];
        subscription = context.calendarEnumerableQuery.subscribe(
            result => received.push(result),
            { category: 'test-category' });

        fakeEventSource.onmessage!({
            data: JSON.stringify({
                data: [{ children: [{ date: '2026-02-03', time: '14:15:16' }] }],
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
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
    it('should deserialize the child date', () => received[0].data[0].children[0].date.should.be.instanceOf(DateOnly));
    it('should preserve the child date', () => received[0].data[0].children[0].date.equals(DateOnly.from(2026, 2, 3)).should.be.true);
    it('should deserialize the child time', () => received[0].data[0].children[0].time.should.be.instanceOf(TimeOnly));
    it('should preserve the child time', () => received[0].data[0].children[0].time.equals(TimeOnly.from(14, 15, 16)).should.be.true);
}));
