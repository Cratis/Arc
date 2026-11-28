// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, fireEvent, render, screen } from '@testing-library/react';
import { QueryResult } from '@cratis/arc/queries';
import { it } from 'vitest';
import { ArcContext, type ArcConfiguration } from '../../ArcContext.js';
import { clearSuspenseQueryCache, useSuspenseQuery } from '../useSuspenseQuery.js';
import { FakeSuspenseQuery, type FakeSuspenseQueryResult } from './FakeSuspenseQuery.js';

const configuration = {
    microservice: 'test-microservice',
    httpHeadersCallback: () => ({ 'X-Tenant': 'example' })
};
const equivalentConfiguration = { ...configuration, httpHeadersCallback: () => ({ 'x-tenant': 'example' }) };
type RowsResult = QueryResult<FakeSuspenseQueryResult[]>;
const replies: ((result: RowsResult) => void)[] = [];

class DeferredQuery extends FakeSuspenseQuery {
    override perform(): Promise<RowsResult> {
        return new Promise(resolve => replies.push(resolve));
    }
}

function respond(requestIndex: number, name: string) {
    return act(async () => {
        replies[requestIndex](new QueryResult<FakeSuspenseQueryResult[]>({
            data: [{ id: '1', name }], isSuccess: true, isReady: true, isAuthorized: true,
            isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [],
            exceptionStackTrace: '', paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
        }, Object, true));
    });
}

function Rows({ name }: { name: string }) {
    const [result, perform] = useSuspenseQuery<FakeSuspenseQueryResult[], DeferredQuery>(DeferredQuery);
    return <>
        <section aria-label={name}>{result.data[0]?.name}</section>
        <button onClick={() => void perform()}>Refresh {name}</button>
    </>;
}

function view(names: string[], context: ArcConfiguration = configuration) {
    return <React.StrictMode>
        <ArcContext.Provider value={context}>
            {names.map(name => <React.Suspense key={name} fallback={<span role="status" />}>
                <Rows name={name} />
            </React.Suspense>)}
        </ArcContext.Provider>
    </React.StrictMode>;
}

function rowsShown(name: string) {
    return screen.getByRole('region', { name }).textContent!;
}

describe('when consumers share a query', () => {
    beforeEach(() => { replies.length = 0; });

    afterEach(() => {
        clearSuspenseQueryCache();
    });

    it('should reuse the result after one of its consumers unmounts', async () => {
        const rendered = render(view(['first', 'second']));
        await respond(0, 'shared result');
        await act(async () => { rendered.rerender(view(['second'])); });
        await act(async () => { rendered.rerender(view(['second', 'third'], equivalentConfiguration)); });

        replies.should.have.lengthOf(1);
        rowsShown('second').should.equal('shared result');
        rowsShown('third').should.equal('shared result');
    });

    it.each([
        { lookup: 'the same header source', context: configuration },
        { lookup: 'an equivalent header source that already shared the result', context: equivalentConfiguration },
        { lookup: 'equivalent header values from a new source', context: { ...configuration, httpHeadersCallback: () => ({ 'x-tenant': 'example' }) } },
    ])('should fetch again after the last consumer unmounts with $lookup', async ({ context }) => {
        const rendered = render(view(['first']));
        await respond(0, 'earlier result');
        await act(async () => { rendered.rerender(view(['first'], equivalentConfiguration)); });
        await act(async () => { rendered.rerender(view([])); });
        rendered.rerender(view(['second'], context));

        replies.should.have.lengthOf(2);
        await respond(1, 'fresh result');
        rowsShown('second').should.equal('fresh result');
    });

    it('should fetch fresh rows for each consumer that refreshes', async () => {
        render(view(['first', 'second']));
        await respond(0, 'earlier result');
        fireEvent.click(screen.getByRole('button', { name: 'Refresh first' }));

        replies.should.have.lengthOf(2);
        await respond(1, 'first refresh');
        rowsShown('first').should.equal('first refresh');

        fireEvent.click(screen.getByRole('button', { name: 'Refresh second' }));

        replies.should.have.lengthOf(3);
        await respond(2, 'second refresh');
        rowsShown('second').should.equal('second refresh');
    });
});
