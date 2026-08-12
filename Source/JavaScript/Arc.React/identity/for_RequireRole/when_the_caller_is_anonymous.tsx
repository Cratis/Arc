// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../given';
import { a_role_gate, allowedText, forbiddenText, loadingText } from './given/a_role_gate';

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
    it('should not render the guarded content', () => context.text.should.not.contain(allowedText));
    it('should stop reporting that it is loading', () => context.text.should.not.contain(loadingText));
}));
