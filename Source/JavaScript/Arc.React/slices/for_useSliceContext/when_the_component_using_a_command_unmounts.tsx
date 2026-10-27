// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, render } from '@testing-library/react';
import { useState } from 'react';
import { Slice } from '../Slice.js';
import { useCommand } from '../../commands/useCommand.js';
import { ArcContext } from '../../ArcContext.js';
import { ReportButton } from './ReportButton.js';
import { PayInvoice } from './PayInvoice.js';

const Pay = () => {
    useCommand(PayInvoice);
    return null;
};

let hide: () => void;

const Page = () => {
    const [isShowing, setIsShowing] = useState(true);
    hide = () => setIsShowing(false);
    return <>{isShowing && <Pay />}<ReportButton /></>;
};

describe('when the component using a command unmounts', () => {
    let before: string;
    let after: string;

    beforeEach(() => {
        const result = render(
            <ArcContext.Provider value={{ microservice: '' }}>
                <Slice><Page /></Slice>
            </ArcContext.Provider>);
        before = result.getByTestId('location').textContent!;
        act(() => hide());
        after = result.getByTestId('location').textContent!;
        result.unmount();
    });

    it('should have the location while mounted', () => before.should.equal('Billing/Invoices/Pay'));
    it('should lose the location once unmounted', () => after.should.equal(''));
});
