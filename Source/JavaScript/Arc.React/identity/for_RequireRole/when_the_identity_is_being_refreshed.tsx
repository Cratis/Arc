// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../given';
import { a_role_gate, forbiddenText, loadingText } from './given/a_role_gate';

/**
 * A refresh is how an application picks up a sign-in or a freshly granted role, and for the length of
 * the round-trip the identity in hand is stale. Reporting that as settled moves the flash the gate
 * exists to prevent from first paint to every refresh - the caller would watch the guarded screen turn
 * into the forbidden one and back again.
 */
describe('when the identity is being refreshed', given(a_role_gate, context => {
    beforeEach(async () => {
        context.renderGate({
            roles: ['Administrator'],
            whileLoading: <span>{loadingText}</span>,
            forbidden: <span>{forbiddenText}</span>
        });
        await context.signIn(['Administrator']);
        await context.beginRefresh();
    });

    afterEach(() => context.cleanup());

    it('should render what it was told to render while loading', () => context.text.should.equal(loadingText));
}));
