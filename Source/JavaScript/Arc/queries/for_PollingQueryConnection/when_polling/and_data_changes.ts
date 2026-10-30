// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given.js';
import { a_polling_query_connection } from '../given/a_polling_query_connection.js';

describe('when polling and data changes', given(a_polling_query_connection, context => {
    beforeEach(async () => {
        context.fetchStub.onFirstCall().resolves(new Response(JSON.stringify(context.envelope('first'))));
        context.connection.connect(context.dataReceived, { filter: 'a', page: 1, pageSize: 10 });
        await context.clock.tickAsync(0);
        await context.clock.tickAsync(1000);
    });
    it('should get the query route with the HTTP arguments immediately', () => {
        String(context.fetchStub.firstCall.args[0]).should.equal('https://example.com/api/items/42?filter=a&page=1&pageSize=10&waitForFirstResult=true');
        context.fetchStub.firstCall.args[1].method.should.equal('GET');
    });
    it('should include custom and query headers', () => {
        Object.fromEntries(context.fetchStub.firstCall.args[1].headers).should.deep.equal({
            authorization: 'Bearer x', accept: 'application/json', 'content-type': 'application/json', 'x-cratis-microservice': 'svc'
        });
    });
    it('should deliver changed data and paging metadata', () => {
        context.dataReceived.callCount.should.equal(2);
        context.dataReceived.secondCall.args[0].data.should.equal('same');
        context.dataReceived.secondCall.args[0].paging.totalItems.should.equal(25);
    });
    it('should measure request latency', () => {
        context.connection.lastPingLatency.should.be.at.least(0);
        context.connection.averageLatency.should.be.at.least(0);
    });
}));

for (const changes of [
    { paging: { page: 2, size: 10, totalItems: 30, totalPages: 3 } },
    { isSuccess: false }, { isAuthorized: false }, { isValid: false }, { hasExceptions: true },
    { validationResults: [{ message: 'invalid' }] }, { exceptionMessages: ['failed'] }, { exceptionStackTrace: 'trace' }
]) {
    describe(`when polling and envelope metadata changes: ${Object.keys(changes)[0]}`, given(a_polling_query_connection, context => {
        beforeEach(async () => {
            context.responses = [
                new Response(JSON.stringify(context.envelope('same'))),
                new Response(JSON.stringify(context.envelope('same', changes)))
            ];
            context.connection.connect(context.dataReceived);
            await context.clock.tickAsync(0);
            await context.clock.tickAsync(1000);
        });
        it('should emit changed metadata even when the data is unchanged', () => context.dataReceived.callCount.should.equal(2));
    }));
}
