// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render, screen } from '@testing-library/react';
import { QueryResult } from '@cratis/arc/queries';
import { it } from 'vitest';
import { clearSuspenseQueryCache, useSuspenseQuery } from '../useSuspenseQuery.js';
import { FakeSuspenseQuery, type FakeSuspenseQueryResult } from './FakeSuspenseQuery.js';

type RowsResult = QueryResult<FakeSuspenseQueryResult[]>;
type Reply = (result: RowsResult) => void;

function givenDeferredQuery() {
    const replies: Reply[] = [];
    class DeferredQuery extends FakeSuspenseQuery {
        override perform(): Promise<RowsResult> {
            return new Promise(resolve => replies.push(resolve));
        }
    }
    return {
        replies,
        useRow: () => useSuspenseQuery<FakeSuspenseQueryResult[], DeferredQuery>(DeferredQuery)[0].data[0]?.name,
    };
}

function respond(reply: Reply, name: string) {
    return act(async () => {
        reply(new QueryResult<FakeSuspenseQueryResult[]>({
            data: [{ id: '1', name }], isSuccess: true, isReady: true, isAuthorized: true,
            isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [],
            exceptionStackTrace: '', paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
        }, Object, true));
    });
}

function givenQueries() {
    const first = givenDeferredQuery();
    const second = givenDeferredQuery();

    function FirstRow() { return <span>{first.useRow()}</span>; }
    function SecondRow() { return <span>{second.useRow()}</span>; }
    function Siblings() { return <><FirstRow /><SecondRow /></>; }
    function Sequential() {
        const firstName = first.useRow();
        const secondName = second.useRow();
        return <><span>{firstName}</span><span>{secondName}</span></>;
    }

    return { first, second, Siblings, Sequential };
}

describe('when a boundary waits for two queries', () => {
    beforeEach(() => { vi.useFakeTimers(); });

    afterEach(() => {
        clearSuspenseQueryCache();
        vi.useRealTimers();
    });

    it.each([
        { component: 'Siblings', arrangement: 'sibling components' },
        { component: 'Sequential', arrangement: 'sequential hooks in one component' },
    ] as const)('should render both results without re-fetching when $arrangement finish far apart', async ({ component }) => {
        const queries = givenQueries();
        const Rows = queries[component];
        render(<React.Suspense fallback={<span role="status" />}><Rows /></React.Suspense>);
        screen.getByRole('status');

        await respond(queries.first.replies[0], 'first result');
        await act(async () => { vi.advanceTimersByTime(1500); });
        screen.getByRole('status');
        await respond(queries.second.replies[0], 'second result');

        queries.first.replies.should.have.lengthOf(1);
        queries.second.replies.should.have.lengthOf(1);
        screen.getByText('first result');
        screen.getByText('second result');
    });
});
