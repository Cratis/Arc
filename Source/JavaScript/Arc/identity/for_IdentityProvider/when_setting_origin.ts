// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IdentityProvider } from '../IdentityProvider';
import { an_identity_provider } from './given/an_identity_provider';
import { given } from '../../given';

describe('when setting origin', given(an_identity_provider, () => {
    let origin: string;

    beforeEach(() => {
        origin = 'https://example.com';
        IdentityProvider.setOrigin(origin);
    });

    afterEach(() => {
        IdentityProvider.setOrigin('');
    });

    it('should set the origin', () => IdentityProvider.origin.should.equal(origin));
}));
