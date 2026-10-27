// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Suspense } from 'react';
import { act, render } from '@testing-library/react';
import sinon from 'sinon';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { Slice } from '../Slice.js';
import { useSuspenseQuery, clearSuspenseQueryCache } from '../../queries/useSuspenseQuery.js';
import { ArcContext } from '../../ArcContext.js';
import { ReportButton } from './ReportButton.js';
import { SuspenseInvoices } from './SuspenseInvoices.js';

const Listing = () => {
    useSuspenseQuery(SuspenseInvoices);
    return <ul />;
};

describe('when a suspense query is used in the scope', () => {
    const fetchHelper = createFetchHelper();
    let location: string;

    beforeEach(async () => {
        const fetchStub: sinon.SinonStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            json: async () => ({ data: [], isSuccess: true, isAuthorized: true, isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [], exceptionStackTrace: '' })
        } as Response);

        const result = render(
            <ArcContext.Provider value={{ microservice: '' }}>
                <Slice>
                    <Suspense fallback={null}>
                        <Listing />
                    </Suspense>
                    <ReportButton />
                </Slice>
            </ArcContext.Provider>);
        await act(async () => { await Promise.resolve(); });
        location = result.getByTestId('location').textContent!;
        result.unmount();
    });

    afterEach(() => {
        fetchHelper.restore();
        clearSuspenseQueryCache();
    });

    it('should give other components the location of the query once it resolves', () => location.should.equal('Billing/Invoices/Suspending'));
});
