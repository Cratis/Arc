// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { fireEvent, render, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { CommandForm, useCommandInstance } from '../CommandForm';
import {
    asCommandFormField,
    type InjectedCommandFormFieldProps,
    type WrappedFieldProps,
} from '../asCommandFormField';
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

const OpaqueLayout = ({
    children,
    onValueChange,
}: {
    children: React.ReactElement<InjectedCommandFormFieldProps>;
    onValueChange: (value: unknown) => void;
}) =>
    React.cloneElement(children, {
        onValueChange: (value: unknown) => {
            children.props.onValueChange?.(value);
            onValueChange(value);
        },
    });

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

describe(
    'when an opaque layout supplies onValueChange',
    given(a_command_form_context, (context) => {
        let command: TestCommand;
        let consumerOnValueChange: sinon.SinonSpy;
        let layoutOnValueChange: sinon.SinonSpy;
        let result: ReturnType<typeof render>;

        beforeEach(async () => {
            consumerOnValueChange = sinon.spy();
            layoutOnValueChange = sinon.spy();
            result = render(
                <CommandForm command={TestCommand} currentValues={{ name: 'Before' }}>
                    <CommandProbe capture={(instance) => (command = instance)} />
                    <OpaqueLayout onValueChange={layoutOnValueChange}>
                        <TestField<TestCommand>
                            value={(instance) => instance.name}
                            onValueChange={consumerOnValueChange}
                            testId='opaque-field'
                        />
                    </OpaqueLayout>
                </CommandForm>,
                { wrapper: context.createWrapper() },
            );

            await waitFor(() =>
                (result.getByTestId('opaque-field') as HTMLInputElement).value.should.equal(
                    'Before',
                ),
            );
            fireEvent.change(result.getByTestId('opaque-field'), {
                target: { value: 'After' },
            });
        });

        it('should retain command binding', () => {
            command.name!.should.equal('After');
        });

        it('should invoke the consumer callbacks', () => {
            consumerOnValueChange.calledOnceWithExactly('After').should.equal(true);
            layoutOnValueChange.calledOnceWithExactly('After').should.equal(true);
        });

        it('should render one field and one framework container', () => {
            result.getAllByTestId('opaque-field').should.have.lengthOf(1);
            result.container.querySelectorAll('.w-full').should.have.lengthOf(1);
        });
    }),
);
