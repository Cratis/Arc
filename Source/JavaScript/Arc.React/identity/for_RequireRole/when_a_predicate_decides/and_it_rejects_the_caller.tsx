// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../../given';
import { a_role_gate, allowedText, forbiddenText } from '../given/a_role_gate';

describe('when a predicate decides and it rejects the caller', given(a_role_gate, context => {
    beforeEach(async () => {
        context.renderGate({
            allow: details => details.isConsultant === true,
            forbidden: <span>{forbiddenText}</span>
        });
        await context.signIn([], { isConsultant: false });
    });

    afterEach(() => context.cleanup());

    it('should render the forbidden content', () => context.text.should.equal(forbiddenText));
    it('should not render the guarded content', () => context.text.should.not.contain(allowedText));
}));
