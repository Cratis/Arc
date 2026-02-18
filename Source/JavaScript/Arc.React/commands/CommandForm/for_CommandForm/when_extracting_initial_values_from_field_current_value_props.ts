// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { CommandFormField } from '../CommandFormField';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

describe("when extracting initial values from field currentValue props", given(a_command_form_context, context => {
    let capturedCommand: TestCommand | null = null;

    beforeEach(() => {
        const TestComponent = () => {
            const command = useCommandInstance<TestCommand>();
            capturedCommand = command;
            return React.createElement('div');
        };

        render(
            React.createElement(
                CommandForm,
                { command: TestCommand },
                React.createElement(TestComponent),
                React.createElement(CommandFormField, {
                    value: (c: TestCommand) => c.name,
                    currentValue: 'Field Value',
                    title: 'Name'
                }),
                React.createElement(CommandFormField, {
                    value: (c: TestCommand) => c.email,
                    currentValue: 'field@example.com',
                    title: 'Email'
                })
            ),
            { wrapper: context.createWrapper() }
        );
    });

    it("should set name from field currentValue", () => {
        capturedCommand!.name.should.equal('Field Value');
    });

    it("should set email from field currentValue", () => {
        capturedCommand!.email.should.equal('field@example.com');
    });
}));
