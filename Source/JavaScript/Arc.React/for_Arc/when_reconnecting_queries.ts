// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { startTransition, useContext } from 'react';
import { act, render, screen } from '@testing-library/react';
import { it, vi } from 'vitest';
import { ObservableQuerySubscription, QueryResult, type OnNextResult } from '@cratis/arc/queries';
import { Arc } from '../Arc.js';
import { ArcContext } from '../ArcContext.js';
import { FakeQuery, type FakeQueryResult } from '../queries/for_useQuery/FakeQuery.js';
import { FakeSuspenseObservableQuery } from '../queries/for_useSuspenseObservableQuery/FakeSuspenseObservableQuery.js';
import { useChangeStream } from '../queries/useChangeStream.js';
import { useObservableQuery } from '../queries/useObservableQuery.js';
import { useQuery } from '../queries/useQuery.js';
import { clearSuspenseObservableQueryCache, useSuspenseObservableQuery } from '../queries/useSuspenseObservableQuery.js';
import { clearSuspenseQueryCache, useSuspenseQuery } from '../queries/useSuspenseQuery.js';

function createDeferredQueries() {
    const replies: OnNextResult<QueryResult<FakeQueryResult[]>>[] = [];
    class DeferredQuery extends FakeQuery {
        override perform(): Promise<QueryResult<FakeQueryResult[]>> {
            return new Promise(resolve => replies.push(resolve));
        }
    }
    class DeferredObservableQuery extends FakeSuspenseObservableQuery {
        override subscribe(callback: OnNextResult<QueryResult<FakeQueryResult[]>>) {
            replies.push(callback);
            return new ObservableQuerySubscription<FakeQueryResult[]>({
                connect: vi.fn(), disconnect: vi.fn(), lastPingLatency: 0, averageLatency: 0
            });
        }
    }
    return { DeferredQuery, DeferredObservableQuery, replies };
}

type DeferredQueries = ReturnType<typeof createDeferredQueries>;

function response(name: string) {
    return new QueryResult<FakeQueryResult[]>({
        data: [{ id: '1', name }], isReady: true, isSuccess: true, isAuthorized: true,
        isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [],
        exceptionStackTrace: '', paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
    }, Object, true);
}

function useRegularRows({ DeferredQuery }: DeferredQueries) {
    return useQuery<FakeQueryResult[], FakeQuery>(DeferredQuery)[0].data;
}

function useObservableRows({ DeferredObservableQuery }: DeferredQueries) {
    return useObservableQuery<FakeQueryResult[], FakeSuspenseObservableQuery>(DeferredObservableQuery)[0].data;
}

function useSuspenseRows({ DeferredQuery }: DeferredQueries) {
    return useSuspenseQuery<FakeQueryResult[], FakeQuery>(DeferredQuery)[0].data;
}

function useSuspenseObservableRows({ DeferredObservableQuery }: DeferredQueries) {
    return useSuspenseObservableQuery<FakeQueryResult[], FakeSuspenseObservableQuery>(DeferredObservableQuery)[0].data;
}

function useChanges({ DeferredObservableQuery }: DeferredQueries) {
    const changes = useChangeStream<FakeQueryResult, FakeSuspenseObservableQuery>(DeferredObservableQuery);
    return [
        ...changes.added.map(row => ({ ...row, name: `added ${row.name}` })),
        ...changes.removed.map(row => ({ ...row, name: `removed ${row.name}` })),
        ...changes.replaced.map(row => ({ ...row, name: `replaced ${row.name}` })),
    ];
}

function renderRows(useRows: (queries: DeferredQueries) => FakeQueryResult[], outerBoundary = false) {
    const queries = createDeferredQueries();
    let reconnect: (() => void) | undefined;
    function Controls() {
        reconnect = useContext(ArcContext).reconnectQueries;
        return null;
    }
    function Rows({ name }: { name: string }) {
        const rows = useRows(queries);
        return React.createElement('section', { 'aria-label': name }, rows.map(row => row.name).join(', '));
    }
    const view = (showSecond: boolean) => React.createElement(Arc, null,
        React.createElement(Controls),
        outerBoundary ? React.createElement(Rows, { name: 'first' }) :
            React.createElement(React.Suspense, { fallback: React.createElement('span', { role: 'status' }) }, React.createElement(Rows, { name: 'first' })),
        showSecond && React.createElement(React.Suspense, { fallback: React.createElement('span', { role: 'status' }) }, React.createElement(Rows, { name: 'second' })));
    const rendered = render(outerBoundary
        ? React.createElement(React.Suspense, { fallback: React.createElement('span', { role: 'status' }) }, view(false))
        : view(false));
    return {
        reconnect: () => reconnect!(),
        replies: queries.replies,
        mountSecond: () => rendered.rerender(view(true)),
        unmount: rendered.unmount,
    };
}

function rowsShown(name: string) {
    return screen.queryByRole('region', { name })?.textContent ?? '';
}

const cases = [
    { hook: 'useQuery', useRows: useRegularRows },
    { hook: 'useObservableQuery', useRows: useObservableRows },
    { hook: 'useSuspenseQuery', useRows: useSuspenseRows },
    { hook: 'useSuspenseObservableQuery', useRows: useSuspenseObservableRows },
];

describe('when reconnecting queries', () => {
    beforeEach(() => {
        vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify({ id: '', name: '', roles: [], details: {} }))));
    });

    afterEach(() => {
        clearSuspenseQueryCache();
        clearSuspenseObservableQueryCache();
        vi.unstubAllGlobals();
    });

    it.each(cases)('clears cached results and renders fresh results with $hook', async ({ useRows }) => {
        const view = renderRows(useRows);
        await act(async () => { view.replies[0](response('first result')); });
        rowsShown('first').should.equal('first result');

        await act(async () => { startTransition(view.reconnect); });
        rowsShown('first').should.equal('');
        view.mountSecond();
        rowsShown('second').should.equal('');

        await act(async () => { view.replies.slice(1).forEach(reply => reply(response('fresh result'))); });
        rowsShown('first').should.equal('fresh result');
        rowsShown('second').should.equal('fresh result');
        view.unmount();
    });

    it.each(cases)('ignores pending results from before a reconnect with $hook', async ({ useRows }) => {
        const view = renderRows(useRows);
        await act(async () => { view.reconnect(); });
        const freshReply = view.replies.at(-1);
        (freshReply !== view.replies[0]).should.be.true;

        await act(async () => { view.replies[0](response('earlier result')); });
        rowsShown('first').should.equal('');
        view.mountSecond();
        rowsShown('second').should.equal('');

        await act(async () => { view.replies.slice(1).forEach(reply => reply(response('fresh result'))); });
        rowsShown('first').should.equal('fresh result');
        rowsShown('second').should.equal('fresh result');
        view.unmount();
    });

    it.each(cases.slice(0, 2))('does not replay queued results with $hook', async ({ useRows }) => {
        const view = renderRows(useRows);
        await act(async () => {
            startTransition(() => view.replies[0](response('queued result')));
            // Allow an asynchronous query to settle without flushing the batch.
            await Promise.resolve();
            await Promise.resolve();
            await Promise.resolve();
            view.reconnect();
        });
        rowsShown('first').should.equal('');
        await act(async () => { view.replies.at(-1)!(response('fresh result')); });
        rowsShown('first').should.equal('fresh result');
    });

    it.each(cases.slice(2))('retries the first request with Suspense above Arc and $hook', async ({ useRows }) => {
        const view = renderRows(useRows, true);
        await act(async () => { view.replies[0](response('first result')); });
        rowsShown('first').should.equal('first result');
        view.replies.length.should.equal(1);
    });

    it('does not expose or diff against change-stream rows from before reconnect', async () => {
        const view = renderRows(useChanges);
        await act(async () => { view.replies[0](response('first result')); });
        rowsShown('first').should.equal('added first result');
        await act(async () => { view.reconnect(); });
        rowsShown('first').should.equal('');
        await act(async () => { view.replies.at(-1)!(response('fresh result')); });
        rowsShown('first').should.equal('added fresh result');
    });
});
