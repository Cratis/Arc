// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { SliceLocation } from '@cratis/arc/reflection';
import { Slice } from '../Slice.js';
import { withSliceLocation } from '../withSliceLocation.js';
import { useSliceContext } from '../useSliceContext.js';
import { useCommand } from '../../commands/useCommand.js';
import { ArcContext } from '../../ArcContext.js';
import { ReportButton } from '../for_useSliceContext/ReportButton.js';
import { PayInvoice } from '../for_useSliceContext/PayInvoice.js';

const folder = new SliceLocation('Billing', 'Invoices', '');

const Pay = withSliceLocation(function Pay() {
    return <span data-testid="own">{useSliceContext().toString()}</span>;
}, folder);

const PayWithCommand = withSliceLocation(function PayWithCommand() {
    useCommand(PayInvoice);
    return <span data-testid="own">{useSliceContext().toString()}</span>;
}, folder);

const Elsewhere = withSliceLocation(function Elsewhere() {
    useCommand(PayInvoice);
    return null;
}, new SliceLocation('Sales', '', ''));

const renderText = (element: JSX.Element, testId: string) => {
    const result = render(<ArcContext.Provider value={{ microservice: '' }}>{element}</ArcContext.Provider>);
    const text = result.getByTestId(testId).textContent!;
    result.unmount();
    return text;
};

describe('when wrapping a function component', () => {
    it('should keep the name of the component', () => Pay.name.should.equal('Pay'));
    it('should take the exported name when the component has no uppercase name', () => withSliceLocation(function renderComponent() {
        return null;
    }, folder, 'Pay').name.should.equal('Pay'));
    it('should keep the display name of the component', () => (Pay as { displayName?: string }).displayName!.should.equal('Pay'));
    it('should give the component itself the folder location', () => renderText(<Pay />, 'own').should.equal('Billing/Invoices'));
    it('should register the commands of the component in its folder scope', () => renderText(<PayWithCommand />, 'own').should.equal('Billing/Invoices/Pay'));
    it('should keep its registrations out of the enclosing scope', () => renderText(<Slice><Elsewhere /><ReportButton /></Slice>, 'location').should.equal(''));
});
