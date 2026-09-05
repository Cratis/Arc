// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../given';
import { a_role_gate, forbiddenText } from './given/a_role_gate';

/**
 * The type system rejects this configuration, but `undefined` defeats it - a renamed feature-flag key
 * reaching `roles` compiles and lints. What the gate does with an access rule it never received is
 * therefore its own question, and the answer has to be no.
 */
describe('when no access rule is supplied', given(a_role_gate, context => {
    beforeEach(async () => {
        context.renderGate({ forbidden: <span>{forbiddenText}</span> });
        await context.signIn(['Administrator']);
    });

    afterEach(() => context.cleanup());

    it('should render the forbidden content', () => context.text.should.equal(forbiddenText));
}));
