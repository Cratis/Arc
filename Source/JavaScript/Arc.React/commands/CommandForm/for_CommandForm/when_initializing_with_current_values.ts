// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

describe("when initializing with current values", given(a_command_form_context, context => {
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
                    currentValues: { name: 'Jane', age: 30 }
                },
                React.createElement(TestComponent)
            ),
            { wrapper: context.createWrapper() }
        );
    });

    it("should set name from current values", () => {
        capturedCommand!.name.should.equal('Jane');
    });

    it("should set age from current values", () => {
        capturedCommand!.age.should.equal(30);
    });
}));
