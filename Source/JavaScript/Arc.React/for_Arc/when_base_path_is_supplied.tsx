// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useContext } from 'react';
import { render, waitFor } from '@testing-library/react';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { IdentityProvider as RootIdentityProvider } from '@cratis/arc/identity';
import { Arc } from '../Arc.js';
import { ArcContext, ArcConfiguration } from '../ArcContext.js';

describe('when base path is supplied without an API base path', () => {
    const fetchHelper = createFetchHelper();
    let configuration: ArcConfiguration;
    let originalApiBasePath: string;

    beforeEach(() => {
        originalApiBasePath = RootIdentityProvider.apiBasePath;
        fetchHelper.stubFetch().resolves({
            ok: true,
            json: async () => ({ id: '', name: '', roles: [], details: {} })
        } as Response);
    });

    afterEach(() => {
        fetchHelper.restore();
        RootIdentityProvider.setApiBasePath(originalApiBasePath);
    });

    it('should retain the context value without using it for API requests', async () => {
        const Probe = () => {
            configuration = useContext(ArcContext);
            return <span>Ready</span>;
        };
        const result = render(<Arc basePath="/workbench"><Probe /></Arc>);

        await waitFor(() => result.getByText('Ready'));
        configuration.basePath!.should.equal('/workbench');
        configuration.apiBasePath!.should.equal('');
        RootIdentityProvider.apiBasePath.should.equal('');
        result.unmount();
    });
});
