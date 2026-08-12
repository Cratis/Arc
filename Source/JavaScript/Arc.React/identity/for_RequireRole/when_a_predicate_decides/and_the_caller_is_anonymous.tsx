// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../../given';
import { a_role_gate, allowedText, forbiddenText } from '../given/a_role_gate';

/**
 * An anonymous identity still carries details - an empty object - so a predicate phrased as an
 * absence ("not blocked") answers true for a caller who never signed in. The gate must reject on
 * identity before it ever consults the predicate.
 */
describe('when a predicate decides and the caller is anonymous', given(a_role_gate, context => {
    beforeEach(async () => {
        context.renderGate({
            allow: details => details.isBlocked !== true,
            forbidden: <span>{forbiddenText}</span>
        });
        await context.stayAnonymous();
    });

    afterEach(() => context.cleanup());

    it('should render the forbidden content', () => context.text.should.equal(forbiddenText));
    it('should not render the guarded content', () => context.text.should.not.contain(allowedText));
}));
