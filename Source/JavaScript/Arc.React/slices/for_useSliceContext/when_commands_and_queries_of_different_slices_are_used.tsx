// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import sinon from 'sinon';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { SliceLocation } from '@cratis/arc/reflection';
import { Slice } from '../Slice.js';
import { useQuery } from '../../queries/useQuery.js';
import { useCommand } from '../../commands/useCommand.js';
import { ArcContext } from '../../ArcContext.js';
import { ReportButton } from './ReportButton.js';
import { AllInvoices } from './AllInvoices.js';
import { PayInvoice } from './PayInvoice.js';

const Page = () => {
    useQuery(AllInvoices);
    useCommand(PayInvoice);
    return <ReportButton onRender={rendered => location = rendered} />;
};

let location: SliceLocation;

describe('when commands and queries of different slices are used', () => {
    const fetchHelper = createFetchHelper();

    beforeEach(() => {
        const fetchStub: sinon.SinonStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            json: async () => ({ data: [], isSuccess: true, isAuthorized: true, isValid: true, hasExceptions: false, validationResults: [], exceptionMessages: [], exceptionStackTrace: '' })
        } as Response);

        render(
            <ArcContext.Provider value={{ microservice: '' }}>
                <Slice><Page /></Slice>
            </ArcContext.Provider>).unmount();
    });

    afterEach(() => fetchHelper.restore());

    it('should keep the module they share', () => location.module.should.equal('Billing'));
    it('should keep the feature they share', () => location.feature.should.equal('Invoices'));
    it('should leave the slice empty', () => location.slice.should.equal(''));
});
