// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { Slice } from '../Slice.js';
import { useCommand } from '../../commands/useCommand.js';
import { ArcContext } from '../../ArcContext.js';
import { ReportButton } from './ReportButton.js';
import { PayInvoice } from './PayInvoice.js';

const Pay = () => {
    useCommand(PayInvoice);
    return null;
};

describe('when slice boundaries are nested', () => {
    let outer: string;
    let innerWithoutRegistrations: string;

    beforeEach(() => {
        const result = render(
            <ArcContext.Provider value={{ microservice: '' }}>
                <Slice>
                    <Slice module="Support" feature="Reports" slice="Submit"><Pay /></Slice>
                    <div data-testid="outer"><ReportButton /></div>
                </Slice>
                <Slice>
                    <Pay />
                    <Slice><div data-testid="inner"><ReportButton /></div></Slice>
                </Slice>
            </ArcContext.Provider>);
        outer = result.getByTestId('outer').textContent!;
        innerWithoutRegistrations = result.getByTestId('inner').textContent!;
        result.unmount();
    });

    it('should keep registrations within the boundary they are made in', () => outer.should.equal(''));
    it('should let a boundary without registrations inherit from the enclosing scope', () => innerWithoutRegistrations.should.equal('Billing/Invoices/Pay'));
});
