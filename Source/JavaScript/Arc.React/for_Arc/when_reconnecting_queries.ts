// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { startTransition, useContext } from 'react';
import { act, render, renderHook, screen } from '@testing-library/react';
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

function renderRows(useRows: (queries: DeferredQueries) => FakeQueryResult[], options: {
    boundary?: 'inner' | 'outer' | 'outer-with-reconnect'; queries?: DeferredQueries;
} = {}) {
    const { boundary = 'inner', queries = createDeferredQueries() } = options;
    const outerBoundary = boundary !== 'inner';
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
        !outerBoundary && React.createElement(Controls),
        outerBoundary ? React.createElement(Rows, { name: 'first' }) :
            React.createElement(React.Suspense, { fallback: React.createElement('span', { role: 'status' }) }, React.createElement(Rows, { name: 'first' })),
        showSecond && React.createElement(React.Suspense, { fallback: React.createElement('span', { role: 'status' }) }, React.createElement(Rows, { name: 'second' })));
    const initialView = outerBoundary
        ? React.createElement(React.Suspense, { fallback: React.createElement('span', { role: 'status' }) }, view(false))
        : view(false);
    // A sibling Arc can reconnect while the Arc under the boundary has yet to commit.
    const rendered = render(boundary === 'outer-with-reconnect'
        ? React.createElement(React.Fragment, null, React.createElement(Arc, null, React.createElement(Controls)), initialView)
        : initialView);
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
        const view = renderRows(useRows, { boundary: 'outer' });
        await act(async () => { view.replies[0](response('first result')); });
        rowsShown('first').should.equal('first result');
        view.replies.length.should.equal(1);
    });

    it.each(cases.slice(2))('restarts a pending first request with Suspense above Arc and $hook after reconnect', async ({ useRows }) => {
        const view = renderRows(useRows, { boundary: 'outer-with-reconnect' });
        view.replies.length.should.equal(1);
        rowsShown('first').should.equal('');

        await act(async () => { view.reconnect(); });
        view.replies.length.should.equal(2);
        await act(async () => { view.replies[0](response('earlier result')); });
        rowsShown('first').should.equal('');

        await act(async () => { view.replies[1](response('fresh result')); });
        rowsShown('first').should.equal('fresh result');
    });

    it.each(cases.slice(2))('discards a pending $hook result when the last Arc unmounts', async ({ useRows }) => {
        const queries = createDeferredQueries();
        const first = renderRows(useRows, { queries });
        first.replies.length.should.equal(1);
        rowsShown('first').should.equal('');
        await act(async () => { first.unmount(); });
        await act(async () => { first.replies[0](response('earlier result')); });

        const second = renderRows(useRows, { queries });
        rowsShown('first').should.equal('');
        second.replies.length.should.equal(2);
        await act(async () => { second.replies[1](response('fresh result')); });
        rowsShown('first').should.equal('fresh result');
    });

    it.each([{ state: 'waiting', isEnabled: true }, { state: 'disabled', isEnabled: false }])(
        'keeps the same empty change set across renders while $state, including after reconnect',
        async ({ isEnabled }) => {
            const { DeferredObservableQuery, replies } = createDeferredQueries();
            let reconnect: (() => void) | undefined;
            function Controls() {
                reconnect = useContext(ArcContext).reconnectQueries;
                return null;
            }
            function Wrapper({ children }: React.PropsWithChildren) {
                return React.createElement(Arc, null, React.createElement(Controls), children);
            }
            const { result, rerender } = renderHook(() => useChangeStream<FakeQueryResult, FakeSuspenseObservableQuery>(
                DeferredObservableQuery, undefined, undefined, undefined, isEnabled), { wrapper: Wrapper });
            await act(async () => { });
            const initial = result.current;
            initial.should.deep.equal({ added: [], replaced: [], removed: [] });
            rerender();
            result.current.should.equal(initial);

            if (isEnabled) {
                await act(async () => { replies[0](response('first result')); });
                result.current.added.map(row => row.name).should.deep.equal(['first result']);
            }
            await act(async () => { reconnect!(); });
            const afterReconnect = result.current;
            afterReconnect.should.deep.equal({ added: [], replaced: [], removed: [] });
            rerender();
            result.current.should.equal(afterReconnect);
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
