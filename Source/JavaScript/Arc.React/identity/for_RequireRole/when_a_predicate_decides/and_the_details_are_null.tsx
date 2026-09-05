// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../../given';
import { a_role_gate, forbiddenText } from '../given/a_role_gate';

/**
 * A server that answers with an explicit null for details is not the same shape as one that omits the
 * key: the omission deserializes to undefined, the null stays null. A guard written against only
 * undefined hands the null to the predicate, and a predicate phrased as an absence admits on it.
 */
describe('when a predicate decides and the details are null', given(a_role_gate, context => {
    beforeEach(async () => {
        context.renderGate({
            allow: details => details?.isBlocked !== true,
            forbidden: <span>{forbiddenText}</span>
        });
        await context.signInWithNullDetails(['Administrator']);
    });

    afterEach(() => context.cleanup());

    it('should render the forbidden content', () => context.text.should.equal(forbiddenText));
}));
