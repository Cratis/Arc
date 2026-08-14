// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../given';
import { a_role_gate, forbiddenText, loadingText } from './given/a_role_gate';

/**
 * The loading slot is filled in so that the one assertion tells the three outcomes apart: a gate that
 * never left loading and a gate that denied render different text.
 */
describe('when the caller is anonymous', given(a_role_gate, context => {
    beforeEach(async () => {
        context.renderGate({
            roles: ['Administrator'],
            whileLoading: <span>{loadingText}</span>,
            forbidden: <span>{forbiddenText}</span>
        });
        await context.stayAnonymous();
    });

    afterEach(() => context.cleanup());

    it('should render the forbidden content', () => context.text.should.equal(forbiddenText));
}));
