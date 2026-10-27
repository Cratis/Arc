// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render, waitFor } from '@testing-library/react';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { Arc } from '../Arc.js';
import { useCommand } from '../commands/useCommand.js';
import { useSliceContext } from '../slices/useSliceContext.js';
import { PayInvoice } from '../slices/for_useSliceContext/PayInvoice.js';

const Pay = () => {
    useCommand(PayInvoice);
    return null;
};

const ReportButton = () => <span data-testid="location">{useSliceContext().toString()}</span>;

describe('when a command with a slice location is used within it', () => {
    const fetchHelper = createFetchHelper();

    beforeEach(() => {
        fetchHelper.stubFetch().resolves({
            ok: true,
            json: async () => ({ id: '', name: '', roles: [], details: {} })
        } as Response);
    });

    afterEach(() => fetchHelper.restore());

    it('should give other components rendered within it the location of the command', async () => {
        const result = render(<Arc><Pay /><ReportButton /></Arc>);

        await waitFor(() => result.getByTestId('location').textContent!.should.equal('Billing/Invoices/Pay'));
        result.unmount();
    });
});
