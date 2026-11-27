// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useContext } from 'react';
import { act, render, screen } from '@testing-library/react';
import { it, vi } from 'vitest';
import { getOrCreateMultiplexer, getSharedMultiplexer, ObservableQuerySubscription, QueryResult, resetSharedMultiplexer, type OnNextResult } from '@cratis/arc/queries';
import { Arc } from '../Arc.js';
import { ArcContext } from '../ArcContext.js';
import { FakeSuspenseObservableQuery, type FakeSuspenseObservableQueryResult } from '../queries/for_useSuspenseObservableQuery/FakeSuspenseObservableQuery.js';
import { useObservableQuery } from '../queries/useObservableQuery.js';
import { useSuspenseObservableQuery } from '../queries/useSuspenseObservableQuery.js';

type Rows = FakeSuspenseObservableQueryResult[];
type Reply = OnNextResult<QueryResult<Rows>>;

function createHub() {
    const subscriptions = new Map<string, Reply>();
    return {
        subscriptions,
        subscribe: (id: string, _request: unknown, reply: Reply) => { subscriptions.set(id, reply); },
        unsubscribe: (id: string) => { subscriptions.delete(id); },
        dispose: () => subscriptions.clear(),
        queryCount: 0, isConnected: true, lastPingLatency: 0, averageLatency: 0,
    };
}

function createQueries() {
    const hubs: ReturnType<typeof createHub>[] = [];
    function newHub() {
        const hub = createHub();
        hubs.push(hub);
        return hub;
    }
    class MultiplexedQuery extends FakeSuspenseObservableQuery {
        override subscribe(callback: Reply) {
            const pool = getOrCreateMultiplexer(newHub, 'spec', 1);
            const unsubscribe = pool.subscribe('Rows', undefined, callback);
            return new ObservableQuerySubscription<Rows>({
                connect: vi.fn(), disconnect: unsubscribe, lastPingLatency: 0, averageLatency: 0,
            });
        }
    }
    return { MultiplexedQuery, hubs };
}

function response(name: string) {
    return new QueryResult<Rows>({
        data: [{ id: '1', name }], isReady: true, isSuccess: true, isAuthorized: true,
        isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [],
        exceptionStackTrace: '', paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
    }, Object, true);
}

const observableHooks = [
    { hook: 'useObservableQuery', useRows: useObservableQuery<Rows, FakeSuspenseObservableQuery> },
    { hook: 'useSuspenseObservableQuery', useRows: useSuspenseObservableQuery<Rows, FakeSuspenseObservableQuery> },
];

describe('when unmounting Arc with a shared multiplexer', () => {
    beforeEach(() => {
        vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify({ id: '', name: '', roles: [], details: {} }))));
    });

    afterEach(() => {
        resetSharedMultiplexer();
        vi.unstubAllGlobals();
    });

    it('keeps the multiplexer through StrictMode replay but closes it on unmount', async () => {
        const dispose = vi.fn();
        const multiplexer = getOrCreateMultiplexer(() => ({
            dispose, subscribe: vi.fn(), unsubscribe: vi.fn(), queryCount: 0,
            isConnected: true, lastPingLatency: 0, averageLatency: 0
        }), 'test', 1);

        const view = render(React.createElement(React.StrictMode, null, React.createElement(Arc)));
        await act(async () => { await new Promise(resolve => setTimeout(resolve, 0)); });
        (getSharedMultiplexer() === multiplexer).should.be.true;
        dispose.mock.calls.length.should.equal(0);

        await act(async () => { view.unmount(); });
        (getSharedMultiplexer() === undefined).should.be.true;
        dispose.mock.calls.length.should.equal(1);
    });

    it.each(observableHooks)('keeps a surviving Arc subscribed with $hook when another Arc unmounts', async ({ useRows }) => {
        const { MultiplexedQuery, hubs } = createQueries();
        function Rows() {
            const [result] = useRows(MultiplexedQuery);
            return React.createElement('output', null, result.data[0]?.name);
        }
        const view = (includeNested: boolean) => React.createElement(Arc, null,
            React.createElement(React.Suspense, { fallback: null }, React.createElement(Rows)),
            includeNested && React.createElement(Arc));
        const rendered = render(view(true));
        await act(async () => { hubs[0].subscriptions.forEach(reply => reply(response('first result'))); });
        screen.getByRole('status').textContent!.should.equal('first result');
        await act(async () => { rendered.rerender(view(false)); });
        await act(async () => { hubs[0].subscriptions.forEach(reply => reply(response('still subscribed'))); });
        screen.getByRole('status').textContent!.should.equal('still subscribed');
    });

    it('keeps a render-time subscription alive when Arc is replaced in the same commit', async () => {
        const { MultiplexedQuery, hubs } = createQueries();
        function Rows() {
            const [result] = useSuspenseObservableQuery<Rows, FakeSuspenseObservableQuery>(MultiplexedQuery);
            return React.createElement('output', null, result.data[0]?.name);
        }
        const view = (key: string) => React.createElement(Arc, { key },
            React.createElement(React.Suspense, { fallback: 'Waiting for rows' }, React.createElement(Rows)));
        const rendered = render(view('old'));
        await act(async () => { rendered.rerender(view('new')); });
        await act(async () => { hubs[0].subscriptions.forEach(reply => reply(response('replacement ready'))); });
        (screen.queryByText('replacement ready') !== null).should.be.true;
    });

    it.each(observableHooks)('reconnects surviving providers using $hook and ignores the old delivery', async ({ useRows }) => {
        const { MultiplexedQuery, hubs } = createQueries();
        let reconnect: (() => void) | undefined;
        function Controls() {
            reconnect = useContext(ArcContext).reconnectQueries;
            return null;
        }
        function Rows() {
            const [result] = useRows(MultiplexedQuery);
            return React.createElement('output', null, result.data[0]?.name);
        }
        render(React.createElement(Arc, null,
            React.createElement(Arc, null, React.createElement(Controls)),
            React.createElement(React.Suspense, { fallback: null }, React.createElement(Rows))));
        const oldReply = [...hubs[0].subscriptions.values()][0];
        await act(async () => { oldReply(response('first result')); });
        screen.getByRole('status').textContent!.should.equal('first result');
        await act(async () => { reconnect!(); oldReply(response('late result')); });
        (screen.queryByRole('status')?.textContent ?? '').should.equal('');
        await act(async () => { hubs.at(-1)!.subscriptions.forEach(reply => reply(response('fresh result'))); });
        screen.getByRole('status').textContent!.should.equal('fresh result');
    });
});
