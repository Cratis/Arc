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

describe('when providers use different request headers', () => {
    const requestedTenants: (string | null)[] = [];
    const firstHeaders = () => ({ 'X-Tenant': 'first tenant' });
    const secondHeaders = () => ({ 'X-Tenant': 'second tenant' });

    beforeEach(() => {
        requestedTenants.length = 0;
        vi.stubGlobal('fetch', vi.fn(async (input: URL | string, init?: RequestInit) => {
            if (!String(input).includes('/api/fake-suspense-query')) {
                return new Response(JSON.stringify({ id: '', name: '', roles: [], details: {} }));
            }
            const tenant = new Headers(init?.headers).get('X-Tenant');
            requestedTenants.push(tenant);
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

    function view() {
        return <>
            <Arc httpHeadersCallback={firstHeaders}>
                <React.Suspense fallback="Loading first"><Rows name="first" /></React.Suspense>
            </Arc>
            <Arc httpHeadersCallback={secondHeaders}>
                <React.Suspense fallback="Loading second"><Rows name="second" /></React.Suspense>
            </Arc>
        </>;
    }

    it('should send the headers and render the response for each provider', async () => {
        render(view());
        const first = await screen.findByRole('region', { name: 'first' });
        const second = await screen.findByRole('region', { name: 'second' });

        requestedTenants.should.have.members(['first tenant', 'second tenant']);
        first.textContent!.should.equal('first tenant');
        second.textContent!.should.equal('second tenant');
    });

    it('should reuse results when the providers rerender with the same header callbacks', async () => {
        const rendered = render(view());
        await screen.findByRole('region', { name: 'first' });
        await screen.findByRole('region', { name: 'second' });
        await act(async () => { rendered.rerender(view()); });

        requestedTenants.should.have.members(['first tenant', 'second tenant']);
    });
});
