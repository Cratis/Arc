// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import sinon from 'sinon';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { Slice } from '../Slice.js';
import { useQuery } from '../../queries/useQuery.js';
import { ArcContext } from '../../ArcContext.js';
import { ReportButton } from './ReportButton.js';
import { AllInvoices } from './AllInvoices.js';

const Listing = () => {
    useQuery(AllInvoices);
    return <ul />;
};

describe('when a query is used in the scope', () => {
    const fetchHelper = createFetchHelper();
    let location: string;

    beforeEach(() => {
        const fetchStub: sinon.SinonStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            json: async () => ({ data: [], isSuccess: true, isAuthorized: true, isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [], exceptionStackTrace: '' })
        } as Response);

        const result = render(
            <ArcContext.Provider value={{ microservice: '' }}>
                <Slice>
                    <Listing />
                    <ReportButton />
                </Slice>
            </ArcContext.Provider>);
        location = result.getByTestId('location').textContent!;
        result.unmount();
    });

    afterEach(() => fetchHelper.restore());

    it('should give other components the location of the query', () => location.should.equal('Billing/Invoices/Listing'));
});
