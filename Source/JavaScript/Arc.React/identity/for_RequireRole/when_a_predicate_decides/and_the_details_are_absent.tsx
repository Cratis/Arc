// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../../given';
import { a_role_gate, forbiddenText } from '../given/a_role_gate';

/**
 * An application with no IProvideIdentityDetails registered gets an identity that is set and carries
 * nothing. A predicate phrased as an absence reads that as innocence and lets the caller in - which is
 * why the gate answers for itself instead of handing an empty hand to a rule it cannot vet.
 */
describe('when a predicate decides and the details are absent', given(a_role_gate, context => {
    beforeEach(async () => {
        context.renderGate({
            allow: details => details?.isBlocked !== true,
            forbidden: <span>{forbiddenText}</span>
        });
        await context.signInWithoutDetails(['Administrator']);
    });

    afterEach(() => context.cleanup());

    it('should render the forbidden content', () => context.text.should.equal(forbiddenText));
}));
