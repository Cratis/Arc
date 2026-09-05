// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { given } from '../../given';
import { a_role_gate, forbiddenText, loadingText } from './given/a_role_gate';

describe('when the identity has not arrived', given(a_role_gate, context => {
    beforeEach(() => {
        context.renderGate({
            roles: ['Administrator'],
            whileLoading: <span>{loadingText}</span>,
            forbidden: <span>{forbiddenText}</span>
        });
    });

    afterEach(() => context.cleanup());

    it('should render what it was told to render while loading', () => context.text.should.equal(loadingText));
}));
