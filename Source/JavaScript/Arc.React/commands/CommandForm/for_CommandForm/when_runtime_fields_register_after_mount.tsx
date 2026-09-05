// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type React from 'react';
import { render, waitFor } from '@testing-library/react';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

interface TestFieldProps extends WrappedFieldProps<string> {
    testId: string;
}

const TestField = asCommandFormField<TestFieldProps>(
    (props: TestFieldProps) => (
        <input
            data-testid={props.testId}
            value={props.value}
            onChange={(event) => props.onChange(event)}
        />
    ),
    {
        defaultValue: '',
        extractValue: (event) =>
            (event as React.ChangeEvent<HTMLInputElement>).target.value,
    },
);

const OpaqueLayout = ({ children }: { children: React.ReactNode }) => (
    <section>{children}</section>
);

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

describe(
    'when runtime fields register after mount',
    given(a_command_form_context, (context) => {
        let command: TestCommand;

        beforeEach(async () => {
            render(
                <CommandForm command={TestCommand}>
                    <CommandProbe capture={(instance) => (command = instance)} />
                    <TestField<TestCommand>
                        value={(instance) => instance.name}
                        currentValue='Direct seed'
                        testId='name-field'
                    />
                    <OpaqueLayout>
                        <TestField<TestCommand>
                            value={(instance) => instance.email}
                            currentValue='nested@example.com'
                            testId='email-field'
                        />
                    </OpaqueLayout>
                </CommandForm>,
                { wrapper: context.createWrapper() },
            );

            await waitFor(() => {
                command.name!.should.equal('Direct seed');
                command.email!.should.equal('nested@example.com');
            });
        });

        it('should establish direct and nested values as a pristine baseline', () => {
            command.hasChanges.should.equal(false);
        });
    }),
);
