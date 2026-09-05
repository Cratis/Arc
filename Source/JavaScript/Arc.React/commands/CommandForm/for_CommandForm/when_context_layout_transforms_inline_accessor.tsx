// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
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
        <input data-testid={props.testId} value={props.value} onChange={props.onChange} />
    ),
    { defaultValue: '' },
);

let layoutRenderCount = 0;

const ContextTransformingLayout = ({
    children,
}: {
    children: React.ReactElement<TestFieldProps>;
}) => {
    useCommandInstance<TestCommand>();
    layoutRenderCount++;
    // SAFETY: The public field element carries command-binding props that are intentionally
    // omitted from the wrapped input component's own value prop type.
    const field = children as unknown as React.ReactElement<{
        value: (command: TestCommand) => unknown;
        currentValue?: unknown;
        initialValue?: (source: unknown) => unknown;
    }>;
    return React.cloneElement(field, {
        value: (command: TestCommand) => command.email,
        currentValue: 'transformed@example.com',
        initialValue: (source: unknown) => (source as { email?: string }).email,
    });
};

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

describe(
    'when a context consuming layout transforms an inline accessor',
    given(a_command_form_context, (context) => {
        let command: TestCommand;

        beforeEach(async () => {
            layoutRenderCount = 0;
            render(
                <CommandForm command={TestCommand}>
                    <CommandProbe capture={(instance) => (command = instance)} />
                    <ContextTransformingLayout>
                        <TestField<TestCommand>
                            value={(instance) => instance.name}
                            testId='transformed-field'
                        />
                    </ContextTransformingLayout>
                </CommandForm>,
                { wrapper: context.createWrapper() },
            );

            await waitFor(() => command.email!.should.equal('transformed@example.com'));
        });

        it('should use the rendered accessor as the population source', () => {
            (command.name === undefined).should.equal(true);
            command.email!.should.equal('transformed@example.com');
            command.hasChanges.should.equal(false);
        });

        it('should settle without a registration render loop', () => {
            layoutRenderCount.should.be.lessThan(10);
        });
    }),
);
