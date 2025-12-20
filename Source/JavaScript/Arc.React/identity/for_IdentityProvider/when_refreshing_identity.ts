// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { IdentityProvider } from '../IdentityProvider';
import { useIdentity } from '../useIdentity';
import { IIdentity } from '@cratis/arc/identity';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';

describe('when refreshing identity', async () => {
    let capturedIdentity: IIdentity | null = null;
    let renderCount = 0;
    let initialRenderCount = 0;
    let reRendered = false;
    let newId = '';
    let newName = '';

    const TestComponent = () => {
        renderCount++;
        capturedIdentity = useIdentity();
        return React.createElement('div', null, 'Test');
    };

    const fetchHelper = createFetchHelper();
    const mockFetch = fetchHelper.stubFetch();
    mockFetch.onFirstCall().resolves({
        json: async () => ({
            id: 'initial-id',
            name: 'Initial User',
            details: { role: 'user' }
        })
    } as Response);
    mockFetch.onSecondCall().resolves({
        json: async () => ({
            id: 'new-id',
            name: 'Updated User',
            details: { role: 'admin' }
        })
    } as Response);

    render(React.createElement(IdentityProvider, null, React.createElement(TestComponent)));

    await new Promise(resolve => setTimeout(resolve, 100));
    
    initialRenderCount = renderCount;
    
    await capturedIdentity!.refresh();
    await new Promise(resolve => setTimeout(resolve, 100));

    reRendered = renderCount > initialRenderCount;
    newId = capturedIdentity!.id;
    newName = capturedIdentity!.name;

    it('should trigger re-render', () => reRendered.should.be.true);
    it('should have new id', () => newId.should.equal('new-id'));
    it('should have new name', () => newName.should.equal('Updated User'));

    fetchHelper.restore();
});
