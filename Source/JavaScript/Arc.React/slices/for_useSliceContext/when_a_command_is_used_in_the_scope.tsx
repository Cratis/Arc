// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { Slice } from '../Slice.js';
import { useCommand } from '../../commands/useCommand.js';
import { ArcContext } from '../../ArcContext.js';
import { ReportButton } from './ReportButton.js';
import { PayInvoice } from './PayInvoice.js';

const PayButton = () => {
    useCommand(PayInvoice);
    return <button>Pay</button>;
};

const Toolbar = (props: { children?: React.ReactNode }) => <div><div>{props.children}</div></div>;

describe('when a command is used in the scope', () => {
    let sibling: string;
    let nested: string;

    beforeEach(() => {
        const result = render(
            <ArcContext.Provider value={{ microservice: '' }}>
                <Slice>
                    <PayButton />
                    <div data-testid="sibling"><ReportButton /></div>
                    <Toolbar><div data-testid="nested"><ReportButton /></div></Toolbar>
                </Slice>
            </ArcContext.Provider>);
        sibling = result.getByTestId('sibling').textContent!;
        nested = result.getByTestId('nested').textContent!;
        result.unmount();
    });

    it('should give a sibling component the location of the command', () => sibling.should.equal('Billing/Invoices/Pay'));
    it('should give a shared component nested in another the location of the command', () => nested.should.equal('Billing/Invoices/Pay'));
});
