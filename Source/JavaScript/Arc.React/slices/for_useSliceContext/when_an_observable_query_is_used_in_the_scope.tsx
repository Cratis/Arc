// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { Slice } from '../Slice.js';
import { useObservableQuery } from '../../queries/useObservableQuery.js';
import { ArcContext } from '../../ArcContext.js';
import { ReportButton } from './ReportButton.js';
import { ObserveInvoices } from './ObserveInvoices.js';

const Observing = () => {
    useObservableQuery(ObserveInvoices);
    return <ul />;
};

describe('when an observable query is used in the scope', () => {
    let location: string;

    beforeEach(() => {
        ObserveInvoices.reset();
        const result = render(
            <ArcContext.Provider value={{ microservice: '' }}>
                <Slice>
                    <Observing />
                    <ReportButton />
                </Slice>
            </ArcContext.Provider>);
        location = result.getByTestId('location').textContent!;
        result.unmount();
    });

    it('should give other components the location of the query', () => location.should.equal('Billing/Invoices/Observing'));
});
