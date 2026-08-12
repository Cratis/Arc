// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../../given';
import { a_role_gate, forbiddenText } from '../given/a_role_gate';

/**
 * A predicate reaching into a shape the identity does not have throws, and a throw out of render is a
 * white screen rather than an answer. The gate turns it into the only answer a gate is allowed to give
 * when it cannot decide.
 */
describe('when a predicate decides and it throws', given(a_role_gate, context => {
    beforeEach(async () => {
        context.renderGate({
            allow: details => (details!.organization as { isActive: boolean }).isActive,
            forbidden: <span>{forbiddenText}</span>
        });
        await context.signIn(['Administrator'], { isConsultant: true });
    });

    afterEach(() => context.cleanup());

    it('should render the forbidden content', () => context.text.should.equal(forbiddenText));
}));
