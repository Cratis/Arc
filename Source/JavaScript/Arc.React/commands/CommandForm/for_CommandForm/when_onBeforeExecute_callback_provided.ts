// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm, useCommandFormContext } from '../CommandForm';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

describe("when onBeforeExecute callback provided", given(a_command_form_context, context => {
    let contextValue: ReturnType<typeof useCommandFormContext> | null = null;

    beforeEach(() => {
        const TestComponent = () => {
            contextValue = useCommandFormContext();
            return React.createElement('div');
        };

        render(
            React.createElement(
                CommandForm,
                {
                    command: TestCommand,
                    onBeforeExecute: (values) => {
                        return { ...values, name: 'Modified' } as TestCommand;
                    }
                },
                React.createElement(TestComponent)
            ),
            { wrapper: context.createWrapper() }
        );
    });

    it("should set onBeforeExecute in context", () => {
        contextValue!.onBeforeExecute.should.not.be.undefined;
    });

    it("should execute callback correctly", () => {
        const result = contextValue!.onBeforeExecute!({ name: 'Original', email: 'test@example.com' } as TestCommand);
        result.name.should.equal('Modified');
    });
}));
