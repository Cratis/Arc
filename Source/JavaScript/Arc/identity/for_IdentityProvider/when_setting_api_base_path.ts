// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../IdentityProvider.js';
import { an_identity_provider } from './given/an_identity_provider.js';
import { given } from '../../given.js';

describe('when setting api base path', given(an_identity_provider, () => {
    let apiBasePath: string;

    beforeEach(() => {
        apiBasePath = '/custom/api';
        IdentityProvider.setApiBasePath(apiBasePath);
    });

    afterEach(() => {
        IdentityProvider.setApiBasePath('');
    });

    it('should set the api base path', () => IdentityProvider.apiBasePath.should.equal(apiBasePath));
}));
