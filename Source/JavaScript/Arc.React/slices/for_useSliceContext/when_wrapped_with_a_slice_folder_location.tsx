// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { SliceLocation } from '@cratis/arc/reflection';
import { Slice } from '../Slice.js';
import { withSliceLocation } from '../withSliceLocation.js';
import { useCommand } from '../../commands/useCommand.js';
import { ArcContext } from '../../ArcContext.js';
import { ReportButton } from './ReportButton.js';
import { PayInvoice } from './PayInvoice.js';

const folder = new SliceLocation('Billing', 'Invoices', '');

const Pay = () => {
    useCommand(PayInvoice);
    return null;
};

const FeaturePage = withSliceLocation((props: { withPay: boolean }) => <>{props.withPay && <Pay />}<ReportButton /></>, folder);

const OtherFolderPage = withSliceLocation(() => <><Pay /><ReportButton /></>, new SliceLocation('Sales', 'Orders', 'Place'));

const ArchivePage = withSliceLocation(() => <Slice slice="Archive"><ReportButton /></Slice>, folder);

const renderLocation = (element: JSX.Element) => {
    const result = render(<ArcContext.Provider value={{ microservice: '' }}>{element}</ArcContext.Provider>);
    const location = result.getByTestId('location').textContent!;
    result.unmount();
    return location;
};

describe('when wrapped with a slice folder location', () => {
    it('should use the folder location', () => renderLocation(<FeaturePage withPay={false} />).should.equal('Billing/Invoices'));
    it('should be refined by a command below the folder', () => renderLocation(<FeaturePage withPay={true} />).should.equal('Billing/Invoices/Pay'));
    it('should not be refined by a command outside the folder', () => renderLocation(<OtherFolderPage />).should.equal('Sales/Orders/Place'));
    it('should lose to an enclosing explicit override', () => renderLocation(<Slice module="Support" feature="Reports"><FeaturePage withPay={true} /></Slice>).should.equal('Support/Reports'));
    it('should lose to an explicit override within it', () => renderLocation(<ArchivePage />).should.equal('Archive'));
});
