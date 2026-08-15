// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { asCommandFormField } from '../asCommandFormField';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';
import { FakePopulateQuery, FakePopulateQueryResult } from '../for_usePopulateFromQuery/FakePopulateQuery';

const SimpleTextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; onBlur?: () => void; invalid: boolean; required: boolean; errors: string[] }>(
    (props) => React.createElement('input', { type: 'text', value: props.value, onChange: props.onChange, 'data-testid': 'field' }),
    { defaultValue: '', extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value }
);

describe('when a field overrides its population', given(a_command_form_context, context => {
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let capturedCommand: TestCommand | null = null;

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        fetchHelper.stubFetch().resolves({
            json: async () => ({
                data: { name: 'Jane Austen', email: 'jane@example.com' },
                isSuccess: true, isAuthorized: true, isValid: true, hasExceptions: false,
                validationResults: [], exceptionMessages: [], exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
            })
        } as Response);

        const TestComponent = () => {
            capturedCommand = useCommandInstance<TestCommand>();
            return React.createElement('div');
        };

        render(
            React.createElement(
                CommandForm,
                { command: TestCommand, populateFromQuery: FakePopulateQuery },
                React.createElement(TestComponent),
                React.createElement(SimpleTextField, {
                    value: (c: TestCommand) => c.name,
                    title: 'Name',
                    initialValue: (source: FakePopulateQueryResult) => source.name.toUpperCase()
                })
            ),
            { wrapper: context.createWrapper() }
        );

        await waitFor(() => (capturedCommand!.name === 'JANE AUSTEN').should.be.true);
    });

    afterEach(() => fetchHelper.restore());

    it('should apply the composed value instead of the matched-by-name one', () => {
        capturedCommand!.name!.should.equal('JANE AUSTEN');
    });
}));
