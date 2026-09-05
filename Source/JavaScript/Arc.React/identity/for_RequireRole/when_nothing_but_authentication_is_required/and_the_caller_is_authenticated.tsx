// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../../given';
import { a_role_gate, allowedText, forbiddenText } from '../given/a_role_gate';

/**
 * The counterpart to a gate with no access rule at all: an application that really does want every
 * signed-in caller through says so, and then gets it.
 */
describe('when nothing but authentication is required and the caller is authenticated', given(a_role_gate, context => {
    beforeEach(async () => {
        context.renderGate({
            allow: () => true,
            forbidden: <span>{forbiddenText}</span>
        });
        await context.signIn([]);
    });

    afterEach(() => context.cleanup());

    it('should render the guarded content', () => context.text.should.equal(allowedText));
}));
