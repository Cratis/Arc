// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { IdentityProviderContext } from '../IdentityProvider';
import { RequireRole } from '../RequireRole';

/**
 * A context composed by hand - a spec, a Storybook decorator - carries no answer to "are you still
 * fetching". Reading that absence as "still fetching" would hold the gate on its loading slot forever
 * for an identity that is in fact resolved, so absent has to read as resolved.
 */
describe('when the context was built by hand', () => {
    const identity = {
        id: 'user-1',
        name: 'A User',
        roles: ['Administrator'],
        details: {},
        isSet: true,
        isInRole: (role: string) => role === 'Administrator',
        refresh: () => Promise.reject(new Error('not used'))
    };

    const text = render(
        <IdentityProviderContext.Provider value={{ identity, clearIdentity: () => { /* not used */ } }}>
            <RequireRole roles={['Administrator']} whileLoading={<span>loading</span>}>
                <span>allowed</span>
            </RequireRole>
        </IdentityProviderContext.Provider>
    ).container.textContent ?? '';

    it('should render the guarded content rather than waiting forever', () => text.should.equal('allowed'));
});
