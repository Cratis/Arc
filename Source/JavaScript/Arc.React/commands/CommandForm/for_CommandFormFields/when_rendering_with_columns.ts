// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField';
import { TestCommand } from '../for_CommandForm/TestCommand';
import { a_command_form_fields_context } from './given/a_command_form_fields_context';
import { given } from '../../../given';

type SimpleTextFieldProps = WrappedFieldProps<string>;

const SimpleTextField = asCommandFormField<SimpleTextFieldProps>(
    (props: SimpleTextFieldProps) => {
        return React.createElement('input', {
            type: 'text',
            value: props.value,
            onChange: props.onChange,
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) =>
            (e as React.ChangeEvent<HTMLInputElement>).target.value,
    },
);

describe(
    'when rendering with columns',
    given(a_command_form_fields_context, (context) => {
        let container: HTMLElement;

        beforeEach(() => {
            const result = render(
                React.createElement(
                    CommandForm,
                    { command: TestCommand },
                    React.createElement(
                        CommandForm.Column,
                        null,
                        React.createElement(SimpleTextField, {
                            value: (c: TestCommand) => c.name,
                            title: 'Name',
                        }),
                    ),
                    React.createElement(
                        CommandForm.Column,
                        null,
                        React.createElement(SimpleTextField, {
                            value: (c: TestCommand) => c.email,
                            title: 'Email',
                        }),
                    ),
                ),
                { wrapper: context.createWrapper() },
            );
            container = result.container;
        });

        it('should render all fields', () => {
            const inputs = container.querySelectorAll('input');
            inputs.should.have.lengthOf(2);
        });

        it('should render fields in columns', () => {
            const columns = container.querySelectorAll('.flex-1');
            columns.should.have.lengthOf(2);
        });

        it('should preserve the columns-only shell shape', () => {
            const card = container.querySelector('.card')!;
            card.classList.contains('flex-wrap').should.equal(false);
            card.children.should.have.lengthOf(2);
        });
    }),
);
