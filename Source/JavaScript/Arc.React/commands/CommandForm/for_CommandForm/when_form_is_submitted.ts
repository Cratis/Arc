// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, screen } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

describe("when form is submitted", given(a_command_form_context, context => {
    let formElement: HTMLFormElement | null;

    beforeEach(() => {
        render(
            React.createElement(
                CommandForm,
                { command: TestCommand },
                React.createElement('button', { type: 'submit', 'data-testid': 'submit-button' }, 'Submit')
            ),
            { wrapper: context.createWrapper() }
        );

        formElement = screen.getByTestId('submit-button').closest('form');
    });

    it('should render a form element', () => expect(formElement).to.not.be.null);
    it('should have submit button inside form', () => {
        const submitButton = screen.getByTestId('submit-button');
        expect(submitButton.type).to.equal('submit');
    });
}));
