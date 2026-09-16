// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, RenderResult, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { DateOnly, field, Guid } from '@cratis/fundamentals';
import { IdentityProvider as RootIdentityProvider } from '@cratis/arc/identity';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { Arc } from '../Arc';
import { useIdentity } from '../identity';

class TheDetails {
    @field(Guid)
    userId!: Guid;

    @field(DateOnly)
    expiration!: DateOnly;
}

const probeTestId = 'probe';
const resolvedText = 'resolved';
const resolvingText = 'resolving';

describe('when a details type is supplied', () => {
    const userId = Guid.create();
    const fetchHelper = createFetchHelper();
    let fetchStub: sinon.SinonStub;
    let renderResult: RenderResult;
    let originalApiBasePath: string;
    let originalOrigin: string;
    let capturedDetails: TheDetails;

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
            const identity = useIdentity<TheDetails>();
            capturedDetails = identity.details;
            return <span data-testid={probeTestId}>{identity.isLoading ? resolvingText : resolvedText}</span>;
        };

        renderResult = render(
            <Arc detailsType={TheDetails} apiBasePath="/api" origin="http://localhost">
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

    it('should deserialize into the supplied type', () => capturedDetails.should.be.instanceOf(TheDetails));
    it('should keep the value the server sent', () => capturedDetails.userId.toString().should.equal(userId.toString()));
    it('should give the details their behavior', () => capturedDetails.expiration.toDate.should.be.a('function'));
});
