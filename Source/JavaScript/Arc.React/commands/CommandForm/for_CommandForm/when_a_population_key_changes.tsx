// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { fireEvent, render, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField';
import { FakePopulateQuery } from '../for_usePopulateFromQuery/FakePopulateQuery';
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

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

const PopulationKeyForm = ({
    capture,
    deriveInitialName,
}: {
    capture: (command: TestCommand) => void;
    deriveInitialName: (locale: string, source: unknown) => string;
}) => {
    const [locale, setLocale] = React.useState('en');
    return (
        <>
            <button type='button' onClick={() => setLocale('nb')}>
                Use Norwegian
            </button>
            <CommandForm command={TestCommand} populateFromQuery={FakePopulateQuery}>
                <CommandProbe capture={capture} />
                <TestField<TestCommand>
                    value={(command) => command.name}
                    initialValue={(source) => deriveInitialName(locale, source)}
                    populationKey={locale}
                    testId='name-field'
                />
            </CommandForm>
        </>
    );
};

describe(
    'when a population key changes',
    given(a_command_form_context, (context) => {
        let command: TestCommand;
        let deriveInitialName: sinon.SinonSpy;
        let result: ReturnType<typeof render>;
        let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

        beforeEach(async () => {
            fetchHelper = createFetchHelper();
            fetchHelper.stubFetch().resolves({
                json: async () => ({
                    data: { name: 'Jane', email: 'jane@example.com' },
                    isSuccess: true,
                    isAuthorized: true,
                    isValid: true,
                    hasExceptions: false,
                    validationResults: [],
                    exceptionMessages: [],
                    exceptionStackTrace: '',
                    paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
                }),
            } as Response);
            deriveInitialName = sinon.spy((locale: string, source: unknown) =>
                locale === 'nb'
                    ? `${(source as { name: string }).name} på norsk`
                    : `${(source as { name: string }).name} in English`,
            );

            result = render(
                <PopulationKeyForm
                    capture={(instance) => (command = instance)}
                    deriveInitialName={deriveInitialName}
                />,
                { wrapper: context.createWrapper() },
            );
            await waitFor(() => command.name!.should.equal('Jane in English'));

            fireEvent.click(result.getByRole('button', { name: 'Use Norwegian' }));
            await waitFor(() => command.name!.should.equal('Jane på norsk'));
        });

        afterEach(() => fetchHelper.restore());

        it('should repopulate with the latest closure', () => {
            command.name!.should.equal('Jane på norsk');
        });

        it('should evaluate the transformed source once per semantic key', () => {
            deriveInitialName.callCount.should.equal(2);
        });

        it('should preserve the repopulated value as the baseline', () => {
            command.hasChanges.should.equal(false);
        });
    }),
);
