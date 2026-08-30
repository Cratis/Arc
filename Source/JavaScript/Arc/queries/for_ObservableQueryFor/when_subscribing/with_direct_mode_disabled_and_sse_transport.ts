// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { an_observable_query_for } from '../given/an_observable_query_for';
import { given } from '../../../given';
import { Globals } from '../../../Globals';
import { QueryTransportMethod } from '../../QueryTransportMethod';
import { ObservableQuerySubscription } from '../../ObservableQuerySubscription';
import { SSE_HUB_ROUTE } from '../../ServerSentEventQueryConnection';
import { resetSharedMultiplexer } from '../../ObservableQueryMultiplexer';
import { HubMessageType } from '../../WebSocketHubConnection';

import * as sinon from 'sinon';

describe(
    'when subscribing with direct mode disabled and SSE transport',
    given(an_observable_query_for, (context) => {
        let callback: sinon.SinonStub;
        let subscription: ObservableQuerySubscription<string>;
        let capturedUrl: string;
        let fetchStub: sinon.SinonStub;
        let eventSourceInstance: EventSource & {
            onopen: (() => void) | null;
            onmessage: ((event: { data: string }) => void) | null;
        };
        let originalQueryDirectMode: boolean;
        let originalTransportMethod: QueryTransportMethod;

        beforeEach(() => {
            originalQueryDirectMode = Globals.queryDirectMode;
            originalTransportMethod = Globals.queryTransportMethod;
            resetSharedMultiplexer();

            Globals.queryDirectMode = false;
            Globals.queryTransportMethod = QueryTransportMethod.ServerSentEvents;

            context.query.setOrigin('https://example.com');
            callback = sinon.stub();

            // EventSource doesn't exist in Node.js — inject a fake via globalThis
            const FakeEventSourceConstructor = function (this: EventSource, url: string) {
                capturedUrl = url;
                eventSourceInstance = this as EventSource & {
                    onopen: (() => void) | null;
                    onmessage: ((event: { data: string }) => void) | null;
                };
                Object.assign(this, {
                    onopen: null,
                    onerror: null,
                    onmessage: null,
                    close: sinon.stub(),
                    addEventListener: sinon.stub(),
                    removeEventListener: sinon.stub(),
                });
            };
            (globalThis as Record<string, unknown>)['EventSource'] =
                FakeEventSourceConstructor;
            fetchStub = sinon.stub().resolves({ ok: true } as Response);
            (globalThis as Record<string, unknown>)['fetch'] = fetchStub;

            subscription = context.query.subscribe(callback, { id: 'test-id' });
            eventSourceInstance.onopen?.();
            // An older Arc server omits subscription capabilities from Connected, so this fixture
            // intentionally exercises legacy missing-revision result frames.
            eventSourceInstance.onmessage?.({
                data: JSON.stringify({
                    type: HubMessageType.Connected,
                    payload: 'connection-1',
                }),
            });

            let subscribeRequest: { queryId: string; revision?: number };
            try {
                subscribeRequest = JSON.parse(fetchStub.firstCall.args[1].body as string);
            } catch (error) {
                throw new Error('The SSE hub subscribe request was not valid JSON', {
                    cause: error,
                });
            }
            eventSourceInstance.onmessage?.({
                data: JSON.stringify({
                    type: HubMessageType.QueryResult,
                    queryId: subscribeRequest.queryId,
                    payload: {
                        data: 'ready result',
                        isSuccess: true,
                        isAuthorized: true,
                        isValid: true,
                        hasExceptions: false,
                        validationResults: [],
                        exceptionMessages: [],
                        exceptionStackTrace: '',
                        paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
                    },
                }),
            });
        });

        afterEach(() => {
            Globals.queryDirectMode = originalQueryDirectMode;
            Globals.queryTransportMethod = originalTransportMethod;
            if (subscription) {
                subscription.unsubscribe();
            }
            delete (globalThis as Record<string, unknown>)['EventSource'];
            delete (globalThis as Record<string, unknown>)['fetch'];
            resetSharedMultiplexer();
        });

        it('should connect to the centralized SSE hub endpoint', () => {
            capturedUrl.should.include(SSE_HUB_ROUTE);
        });

        it('should not include query name in URL since subscriptions are done via POST', () => {
            capturedUrl.should.not.include('query=');
        });

        it('should return a subscription', () => {
            (subscription === undefined).should.equal(false);
        });

        it('should omit revisions when the server does not advertise support', () => {
            (getSubscribeBody().revision === undefined).should.equal(true);
        });

        function getSubscribeBody(): { revision?: number } {
            try {
                return JSON.parse(fetchStub.firstCall.args[1].body as string) as {
                    revision?: number;
                };
            } catch (error) {
                throw new Error('Expected a valid subscribe request body', {
                    cause: error,
                });
            }
        }

        it('should normalize omitted readiness to true', () => {
            callback.firstCall.args[0].isReady.should.equal(true);
        });
    }),
);
