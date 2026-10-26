// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { Slice } from '../Slice.js';
import { useQuery } from '../../queries/useQuery.js';
import { useObservableQuery } from '../../queries/useObservableQuery.js';
import { ArcContext } from '../../ArcContext.js';
import { ReportButton } from './ReportButton.js';
import { AllInvoices } from './AllInvoices.js';
import { ObserveInvoices } from './ObserveInvoices.js';

const Listing = () => {
    useQuery(AllInvoices, undefined, undefined, false);
    useObservableQuery(ObserveInvoices, undefined, undefined, false);
    return <ul />;
};

describe('when a disabled query is used in the scope', () => {
    let location: string;

    beforeEach(() => {
        ObserveInvoices.reset();
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

    it('should not register the location of the query', () => location.should.equal(''));
});
