// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, RenderResult, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { Guid } from '@cratis/fundamentals';
import { IdentityProvider as RootIdentityProvider } from '@cratis/arc/identity';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { Arc } from '../Arc';
import { useIdentity } from '../identity';

interface RawDetails {
    userId: string;
    expiration: string;
}

const probeTestId = 'probe';
const resolvedText = 'resolved';
const resolvingText = 'resolving';

describe('when no details type is supplied', () => {
    const userId = Guid.create();
    const fetchHelper = createFetchHelper();
    let fetchStub: sinon.SinonStub;
    let renderResult: RenderResult;
    let originalApiBasePath: string;
    let originalOrigin: string;
    let capturedDetails: RawDetails;

    beforeEach(async () => {
        originalApiBasePath = RootIdentityProvider.apiBasePath;
        originalOrigin = RootIdentityProvider.origin;

        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            ok: true,
            json: async () => ({
                id: 'user-1',
                name: 'A User',
                roles: [],
                details: { userId: userId.toString(), expiration: '2026-12-31' }
            })
        } as Response);

        const Probe = () => {
            const identity = useIdentity<RawDetails>();
            capturedDetails = identity.details;
            return <span data-testid={probeTestId}>{identity.isLoading ? resolvingText : resolvedText}</span>;
        };

        renderResult = render(
            <Arc apiBasePath="/api" origin="http://localhost">
                <Probe />
            </Arc>
        );

        await waitFor(() => {
            renderResult.getByTestId(probeTestId).textContent!.should.equal(resolvedText);
        });
    });

    afterEach(() => {
        fetchHelper.restore();
        renderResult.unmount();
        RootIdentityProvider.setApiBasePath(originalApiBasePath);
        RootIdentityProvider.setOrigin(originalOrigin);
    });

    it('should hand back the payload untouched', () => {
        capturedDetails.userId.should.equal(userId.toString());
        capturedDetails.expiration.should.equal('2026-12-31');
    });
});
