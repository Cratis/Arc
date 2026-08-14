// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../given';
import { a_role_gate, forbiddenText, loadingText } from './given/a_role_gate';

/**
 * A rejected fetch is a different path from a server answering "not authenticated" - that one resolves
 * and settles the identity on its way through. This one only settles if the failure is handled, and a
 * gate left waiting on an answer that is never coming shows its loading slot forever.
 */
describe('when the identity fails to load', given(a_role_gate, context => {
    beforeEach(async () => {
        context.renderGate({
            roles: ['Administrator'],
            whileLoading: <span>{loadingText}</span>,
            forbidden: <span>{forbiddenText}</span>
        });
        await context.failToLoad();
    });

    afterEach(() => context.cleanup());

    it('should deny the caller rather than leave it loading', () => context.text.should.equal(forbiddenText));
}));
