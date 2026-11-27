// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render, screen } from '@testing-library/react';
import { Arc } from '../../Arc.js';
import { clearSuspenseQueryCache, useSuspenseQuery } from '../useSuspenseQuery.js';
import { FakeSuspenseQuery, type FakeSuspenseQueryResult } from './FakeSuspenseQuery.js';

function Rows({ name }: { name: string }) {
    const [result] = useSuspenseQuery<FakeSuspenseQueryResult[], FakeSuspenseQuery>(FakeSuspenseQuery);
    return <section aria-label={name}>{result.data[0]?.name}</section>;
}

describe('when providers configure request headers', () => {
    const requestedTenants: (string | null)[] = [];
    let waitForQueryResponse: () => Promise<void>;

    beforeEach(() => {
        requestedTenants.length = 0;
        waitForQueryResponse = () => Promise.resolve();
        vi.stubGlobal('fetch', vi.fn(async (input: URL | string, init?: RequestInit) => {
            if (!String(input).includes('/api/fake-suspense-query')) {
                return new Response(JSON.stringify({ id: '', name: '', roles: [], details: {} }));
            }
            const tenant = new Headers(init?.headers).get('X-Tenant');
            requestedTenants.push(tenant);
            await waitForQueryResponse();
            return new Response(JSON.stringify({
                data: [{ id: '1', name: tenant }], isSuccess: true, isReady: true, isAuthorized: true,
                isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [],
                exceptionStackTrace: '', paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
            }));
        }));
    });

    afterEach(() => {
        clearSuspenseQueryCache();
        vi.unstubAllGlobals();
    });

    function provider(name: string, headers: () => HeadersInit) {
        return <Arc httpHeadersCallback={headers}>
            <React.Suspense fallback="Loading"><Rows name={name} /></React.Suspense>
        </Arc>;
    }

    it('should send the headers and render the response for each provider', async () => {
        render(<>
            {provider('first', () => [['X-Tenant', 'first tenant']])}
            {provider('second', () => new Headers({ 'X-Tenant': 'second tenant' }))}
        </>);
        const first = await screen.findByRole('region', { name: 'first' });
        const second = await screen.findByRole('region', { name: 'second' });

        requestedTenants.should.have.members(['first tenant', 'second tenant']);
        first.textContent!.should.equal('first tenant');
        second.textContent!.should.equal('second tenant');
    });

    it('should reuse the result when a new inline callback returns equivalent headers', async () => {
        const rendered = render(provider('first', () => ({ 'X-Tenant': 'first tenant', 'X-Mode': 'read' })));
        await screen.findByRole('region', { name: 'first' });
        await act(async () => {
            rendered.rerender(provider('first', () => [['x-mode', 'read'], ['x-tenant', 'first tenant']]));
        });

        requestedTenants.should.deep.equal(['first tenant']);
        screen.getByRole('region', { name: 'first' }).textContent!.should.equal('first tenant');
    });

    it('should reuse the first request when an outer Suspense boundary retries an inline callback', async () => {
        let resolveFirstRequest!: () => void;
        const firstRequest = new Promise<void>(resolve => { resolveFirstRequest = resolve; });
        const extraRequest = new Promise<void>(() => { /* Keep unexpected requests pending. */ });
        waitForQueryResponse = () => requestedTenants.length === 1 ? firstRequest : extraRequest;

        function App() {
            return <Arc httpHeadersCallback={() => ({ 'X-Tenant': 'first tenant' })}>
                <Rows name="first" />
            </Arc>;
        }

        render(<React.Suspense fallback={<span role="status" />}><App /></React.Suspense>);
        screen.getByRole('status');
        await act(async () => { resolveFirstRequest(); });

        requestedTenants.should.deep.equal(['first tenant']);
        screen.getByRole('region', { name: 'first' }).textContent!.should.equal('first tenant');
    });

    it('should fetch fresh data when the same callback returns changed headers', async () => {
        let tenant = 'first tenant';
        const headers = () => ({ 'X-Tenant': tenant });
        const rendered = render(provider('first', headers));
        const first = await screen.findByRole('region', { name: 'first' });
        first.textContent!.should.equal('first tenant');

        tenant = 'second tenant';
        await act(async () => { rendered.rerender(provider('first', headers)); });

        requestedTenants.should.deep.equal(['first tenant', 'second tenant']);
        screen.getByRole('region', { name: 'first' }).textContent!.should.equal('second tenant');
    });
});
