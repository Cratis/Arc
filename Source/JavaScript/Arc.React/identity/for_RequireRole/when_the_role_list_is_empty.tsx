// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../given';
import { a_role_gate, forbiddenText } from './given/a_role_gate';

/**
 * An empty list is not "no rule" - it is a rule no caller can satisfy. A configuration that produced
 * an empty array must not quietly widen into "everyone".
 */
describe('when the role list is empty', given(a_role_gate, context => {
    beforeEach(async () => {
        context.renderGate({
            roles: [],
            forbidden: <span>{forbiddenText}</span>
        });
        await context.signIn(['Administrator']);
    });

    afterEach(() => context.cleanup());

    it('should render the forbidden content', () => context.text.should.equal(forbiddenText));
}));
