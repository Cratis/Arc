// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { a_server_sent_event_hub_connection } from '../given/a_server_sent_event_hub_connection';
import { given } from '../../../given';
import type { ReconnectCallback } from '../../IReconnectPolicy';

describe(
    'when an immediate SSE open wins a pending backoff race',
    given(a_server_sent_event_hub_connection, (context) => {
        beforeEach(() => {
            context.setup();
            sinon.stub(console, 'warn');
            context.connection.subscribe(
                'query-a',
                { queryName: 'QueryA' },
                sinon.stub(),
            );
            context.simulateError();

            const delayedReconnect = context.policy.schedule.firstCall
                .args[0] as ReconnectCallback;
            context.connection.subscribe(
                'query-b',
                { queryName: 'QueryB' },
                sinon.stub(),
            );
            delayedReconnect();
        });

        afterEach(() => sinon.restore());

        it('should create only the retired and immediate event sources', () =>
            context.eventSources.length.should.equal(2));
        it('should cancel the pending backoff when the immediate open wins', () =>
            context.policy.cancel.called.should.equal(true));
        it('should leave the immediate source connecting', () =>
            context.eventSources[1].readyState.should.equal(EventSource.CONNECTING));
    }),
);
