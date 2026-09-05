// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../../given';
import { a_role_gate, forbiddenText } from '../given/a_role_gate';

/**
 * Gating on authentication alone is the one configuration in which nothing else can stand in for the
 * anonymous check: the predicate says yes to everyone, so an unauthenticated caller reaches the
 * children unless the identity itself is consulted.
 */
describe('when nothing but authentication is required and the caller is anonymous', given(a_role_gate, context => {
    beforeEach(async () => {
        context.renderGate({
            allow: () => true,
            forbidden: <span>{forbiddenText}</span>
        });
        await context.stayAnonymous();
    });

    afterEach(() => context.cleanup());

    it('should render the forbidden content', () => context.text.should.equal(forbiddenText));
}));
