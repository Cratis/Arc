// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { fireEvent, render, waitFor } from '@testing-library/react';
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

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

const ConditionalForm = ({ capture }: { capture: (command: TestCommand) => void }) => {
    const [showEmail, setShowEmail] = React.useState(true);
    return (
        <>
            <button type='button' onClick={() => setShowEmail((value) => !value)}>
                Toggle email
            </button>
            <CommandForm command={TestCommand}>
                <CommandProbe capture={capture} />
                <TestField<TestCommand>
                    value={(command) => command.name}
                    currentValue='Name seed'
                    testId='name-field'
                />
                {showEmail && (
                    <TestField<TestCommand>
                        key='conditional-email'
                        value={(command) => command.email}
                        currentValue='email@example.com'
                        testId='email-field'
                    />
                )}
            </CommandForm>
        </>
    );
};

describe(
    'when a runtime field is removed and remounted',
    given(a_command_form_context, (context) => {
        let command: TestCommand;
        let result: ReturnType<typeof render>;

        beforeEach(async () => {
            result = render(
                <ConditionalForm capture={(instance) => (command = instance)} />,
                {
                    wrapper: context.createWrapper(),
                },
            );
            await waitFor(() => command.email!.should.equal('email@example.com'));

            fireEvent.change(result.getByTestId('name-field'), {
                target: { value: 'User edit' },
            });
            fireEvent.click(result.getByRole('button', { name: 'Toggle email' }));
            await waitFor(() =>
                (result.queryByTestId('email-field') === null).should.equal(true),
            );
            fireEvent.click(result.getByRole('button', { name: 'Toggle email' }));
            await waitFor(() =>
                result.getAllByTestId('email-field').should.have.lengthOf(1),
            );
        });

        it('should preserve unrelated user edits', () => {
            command.name!.should.equal('User edit');
        });

        it('should register only one remounted field', () => {
            result.getAllByTestId('email-field').should.have.lengthOf(1);
            command.email!.should.equal('email@example.com');
        });
    }),
);
