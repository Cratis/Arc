// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../given';
import { a_role_gate, forbiddenText } from './given/a_role_gate';

/**
 * Roles arriving from JSON configuration is the ordinary case, and JSON has a null. The gate has to
 * survive it as a denial rather than throw out of render.
 */
describe('when the roles are not an array', given(a_role_gate, context => {
    beforeEach(async () => {
        context.renderGate({
            roles: null,
            forbidden: <span>{forbiddenText}</span>
        });
        await context.signIn(['Administrator']);
    });

    afterEach(() => context.cleanup());

    it('should render the forbidden content', () => context.text.should.equal(forbiddenText));
}));
