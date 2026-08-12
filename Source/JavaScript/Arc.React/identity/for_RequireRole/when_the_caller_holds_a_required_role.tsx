// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../given';
import { a_role_gate, allowedText, forbiddenText } from './given/a_role_gate';

describe('when the caller holds a required role', given(a_role_gate, context => {
    beforeEach(async () => {
        context.renderGate({
            roles: ['Administrator', 'Auditor'],
            forbidden: <span>{forbiddenText}</span>
        });
        await context.signIn(['Auditor']);
    });

    afterEach(() => context.cleanup());

    it('should render the guarded content', () => context.text.should.equal(allowedText));
    it('should not render the forbidden content', () => context.text.should.not.contain(forbiddenText));
}));
