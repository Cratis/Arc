// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm, useCommandInstance } from '../CommandForm';
// eslint-disable-next-line @typescript-eslint/no-unused-vars
import { CommandFormField } from '../CommandFormField';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

describe("when initial values and current values both provided", given(a_command_form_context, context => {
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
                {
                    command: TestCommand,
                    currentValues: { name: 'FromCurrent', age: 25 },
                    initialValues: { name: 'FromInitial', email: 'initial@example.com' }
                },
                React.createElement(TestComponent)
            ),
            { wrapper: context.createWrapper() }
        );
    });

    it("should prefer initial values over current values for name", () => {
        capturedCommand!.name.should.equal('FromInitial');
    });

    it("should use email from initial values", () => {
        capturedCommand!.email.should.equal('initial@example.com');
    });

    it("should use age from current values when not in initial values", () => {
        capturedCommand!.age.should.equal(25);
    });
}));
