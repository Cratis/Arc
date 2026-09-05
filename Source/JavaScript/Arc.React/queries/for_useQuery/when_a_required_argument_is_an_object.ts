// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { Guid } from '@cratis/fundamentals';
import { QueryInstanceCache } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { useQuery } from '../useQuery';
import { FakeQueryWithObjectArgument, FakeQueryWithObjectArgumentArguments } from './FakeQueryWithObjectArgument';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext';

/**
 * A required parameter whose runtime type is an object - a `Guid`, a `DateOnly`, any generated
 * concept - is the shape a consumer re-derives in render position (`Guid.parse(useParams().id)`).
 * Comparing those by identity re-runs the subscribe effect every render, and that loop sustains
 * itself: each turn aborts the in-flight request and settles a freshly constructed result object,
 * which guarantees the next render. What is pinned here is that the request count does not grow
 * with the number of renders.
 */
describe('when a required argument is an object', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

    const guidValue = '6f9619ff-8b86-d011-b42d-00cf4fc964ff';

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    beforeEach(() => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            json: async () => ({
                data: [],
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
            }),
        } as Response);
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    const wrapper = ({ children }: { children: React.ReactNode }) =>
        React.createElement(
            QueryInstanceCacheContext.Provider,
            { value: new QueryInstanceCache() },
            React.createElement(ArcContext.Provider, { value: config }, children),
        );

    it('should not perform the query again when the argument is re-parsed to an equal value', async () => {
        // Parsed inside the hook callback, so every render hands the hook a brand new - but equal -
        // Guid instance, exactly as a call site parsing a route parameter in render position does.
        const { rerender } = renderHook(
            () =>
                useQuery(FakeQueryWithObjectArgument, {
                    engagementId: Guid.parse(guidValue),
                } as FakeQueryWithObjectArgumentArguments),
            { wrapper },
        );

        await waitFor(() => fetchStub.should.have.been.calledOnce);

        rerender();
        rerender();
        rerender();

        await new Promise((resolve) => setTimeout(resolve, 50));

        fetchStub.should.have.been.calledOnce;
    });

    it('should perform the query again when the argument changes to a different value', async () => {
        const { rerender } = renderHook(
            ({ id }: { id: string }) =>
                useQuery(FakeQueryWithObjectArgument, {
                    engagementId: Guid.parse(id),
                } as FakeQueryWithObjectArgumentArguments),
            { wrapper, initialProps: { id: guidValue } },
        );

        await waitFor(() => fetchStub.should.have.been.calledOnce);

        fetchStub.resetHistory();

        rerender({ id: '11112222-3333-4444-5555-666677778888' });

        await waitFor(() => fetchStub.should.have.been.calledOnce);
    });
});
