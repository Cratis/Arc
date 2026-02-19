// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm, useCommandFormContext } from '../CommandForm';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

describe("when onExecute available in context", given(a_command_form_context, context => {
    let contextOnExecute: (() => Promise<unknown>) | undefined;

    beforeEach(() => {
        const TestComponent = () => {
            const { onExecute } = useCommandFormContext();
            contextOnExecute = onExecute;
            return React.createElement('div');
        };

        render(
            React.createElement(
                CommandForm,
                { command: TestCommand },
                React.createElement(TestComponent)
            ),
            { wrapper: context.createWrapper() }
        );
    });

    it('should have onExecute in context', () => expect(contextOnExecute).to.not.be.undefined);
    it('should be a function', () => expect(typeof contextOnExecute).to.equal('function'));
}));
