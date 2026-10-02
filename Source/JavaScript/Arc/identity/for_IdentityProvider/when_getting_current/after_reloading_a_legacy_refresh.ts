// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';
import { IdentityProvider } from '../../IdentityProvider.js';
import { IIdentity } from '../../IIdentity.js';
import { a_browser_with_a_legacy_identity } from '../given/a_browser_with_a_legacy_identity.js';
import { given } from '../../../given.js';

describe('when getting current after reloading a legacy refresh', given(a_browser_with_a_legacy_identity, context => {
    let identity: IIdentity;

    beforeEach(async () => {
        await IdentityProvider.refresh();
        context.reload();
        vi.resetModules();
        const { IdentityProvider: reloadedProvider } = await import('../../IdentityProvider.js');
        identity = await reloadedProvider.getCurrent();
    });

    it('should ask the server again after the reload', () => context.fetchStub.calledTwice.should.be.true);
    it('should preserve the transition identity across the reload', () => identity.id.should.equal('user-123'));
    it('should preserve the transition roles across the reload', () => identity.isInRole('Reader').should.be.true);
}));
