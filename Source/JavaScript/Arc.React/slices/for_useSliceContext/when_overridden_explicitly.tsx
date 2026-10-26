// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { SliceLocation } from '@cratis/arc/reflection';
import { Slice } from '../Slice.js';
import { useCommand } from '../../commands/useCommand.js';
import { ArcContext } from '../../ArcContext.js';
import { ReportButton } from './ReportButton.js';
import { PayInvoice } from './PayInvoice.js';

const Pay = () => {
    useCommand(PayInvoice);
    return <ReportButton />;
};

describe('when overridden explicitly', () => {
    let fromParts: string;
    let fromLocation: string;
    let partial: string;

    beforeEach(() => {
        const first = render(
            <ArcContext.Provider value={{ microservice: '' }}>
                <Slice module="Support" feature="Reports" slice="Submit"><Pay /></Slice>
            </ArcContext.Provider>);
        fromParts = first.getByTestId('location').textContent!;
        first.unmount();

        const second = render(<Slice location={new SliceLocation('Support', 'Reports', 'Review')}><ReportButton /></Slice>);
        fromLocation = second.getByTestId('location').textContent!;
        second.unmount();

        const third = render(<Slice feature="Reports"><ReportButton /></Slice>);
        partial = third.getByTestId('location').textContent!;
        third.unmount();
    });

    it('should win over the location of a command used within it', () => fromParts.should.equal('Support/Reports/Submit'));
    it('should use a given location', () => fromLocation.should.equal('Support/Reports/Review'));
    it('should leave parts that are not given empty', () => partial.should.equal('Reports'));
});
