// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor } from '@testing-library/react';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { Arc } from '../Arc.js';

describe('when children are not only elements', () => {
    const fetchHelper = createFetchHelper();
    const showDetails = false as boolean;

    beforeEach(() => {
        fetchHelper.stubFetch().resolves({
            ok: true,
            json: async () => ({ id: '', name: '', roles: [], details: {} })
        } as Response);
    });

    afterEach(() => fetchHelper.restore());

    it('should render a string child', async () => {
        const result = render(<Arc>Plain text</Arc>);

        await waitFor(() => result.getByText('Plain text'));
        result.unmount();
    });

    it('should render a mix of elements, strings, conditional children and null', async () => {
        const result = render(
            <Arc>
                <span>Element</span>
                {'Expression'}
                {showDetails && <span>Details</span>}
                {null}
            </Arc>
        );

        await waitFor(() => result.getByText('Element'));
        (result.queryByText('Expression') !== null).should.equal(true);
        (result.queryByText('Details') === null).should.equal(true);
        result.unmount();
    });

    it('should render nothing for a null child', () => {
        const result = render(<Arc>{null}</Arc>);

        result.container.textContent!.should.equal('');
        result.unmount();
    });
});
