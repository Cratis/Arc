// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { fireEvent, render } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

interface StatefulFieldProps extends WrappedFieldProps<string | number> {
    testId: string;
}

let fieldMountCount = 0;
let layoutMountCount = 0;

const StatefulInputComponent = (props: StatefulFieldProps) => {
    const mount = React.useRef(0);
    if (mount.current === 0) {
        mount.current = ++fieldMountCount;
    }
    return (
        <input
            data-testid={props.testId}
            data-mount={mount.current}
            value={props.value}
            onChange={(event) => props.onChange(event)}
        />
    );
};

const StatefulField = asCommandFormField<StatefulFieldProps>(StatefulInputComponent, {
    defaultValue: '',
    extractValue: (event) => (event as React.ChangeEvent<HTMLInputElement>).target.value,
});

const StatefulLayout = () => {
    const mount = React.useRef(0);
    if (mount.current === 0) {
        mount.current = ++layoutMountCount;
    }
    return (
        <div data-testid='stateful-layout' data-mount={mount.current}>
            Layout
        </div>
    );
};

const ReorderableForm = () => {
    const [reordered, setReordered] = React.useState(false);
    const layout = <StatefulLayout key='layout' />;
    const directField = (
        <StatefulField<TestCommand>
            key='direct-field'
            value={(command) => command.name}
            testId='direct-field'
        />
    );
    const emailColumn = (
        <CommandForm.Column key='email-column'>
            <StatefulField<TestCommand>
                key='email-field'
                value={(command) => command.email}
                testId='email-field'
            />
        </CommandForm.Column>
    );
    const ageColumn = (
        <CommandForm.Column key='age-column'>
            <StatefulField<TestCommand>
                key='age-field'
                value={(command) => command.age}
                testId='age-field'
            />
        </CommandForm.Column>
    );
    const plannedChildren = (
        <React.Fragment key='planned-children'>
            {reordered
                ? [emailColumn, layout, ageColumn]
                : [emailColumn, ageColumn, layout]}
        </React.Fragment>
    );
    const children = [directField, plannedChildren];

    return (
        <>
            <button type='button' onClick={() => setReordered((value) => !value)}>
                Reorder
            </button>
            <CommandForm
                command={TestCommand}
                currentValues={{ name: 'Name', email: 'email@example.com', age: 42 }}
            >
                {children}
            </CommandForm>
        </>
    );
};

describe(
    'when keyed command form children are reordered',
    given(a_command_form_context, (context) => {
        let beforeMounts: Record<string, string | null>;
        let afterSplitMounts: Record<string, string | null>;
        let afterRejoinMounts: Record<string, string | null>;

        beforeEach(() => {
            fieldMountCount = 0;
            layoutMountCount = 0;
            const result = render(<ReorderableForm />, {
                wrapper: context.createWrapper(),
            });
            const readMounts = () => ({
                layout: result.getByTestId('stateful-layout').getAttribute('data-mount'),
                direct: result.getByTestId('direct-field').getAttribute('data-mount'),
                email: result.getByTestId('email-field').getAttribute('data-mount'),
                age: result.getByTestId('age-field').getAttribute('data-mount'),
            });

            beforeMounts = readMounts();
            fireEvent.click(result.getByRole('button', { name: 'Reorder' }));
            afterSplitMounts = readMounts();
            fireEvent.click(result.getByRole('button', { name: 'Reorder' }));
            afterRejoinMounts = readMounts();
        });

        it('should preserve state when keyed fragment columns split and rejoin', () => {
            afterSplitMounts.should.deep.equal(beforeMounts);
            afterRejoinMounts.should.deep.equal(beforeMounts);
            layoutMountCount.should.equal(1);
            fieldMountCount.should.equal(3);
        });
    }),
);
