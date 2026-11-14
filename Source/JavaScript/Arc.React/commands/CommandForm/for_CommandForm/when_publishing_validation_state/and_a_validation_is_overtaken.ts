// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render } from '@testing-library/react';
import { CommandResult, type ICommandResult } from '@cratis/arc/commands';
import { ValidationResult, ValidationResultSeverity } from '@cratis/arc/validation';
import { a_command_form_reporting_validation_state, accepted } from './given/a_command_form_reporting_validation_state.js';
import { given } from '../../../../given.js';

const rejected = () => CommandResult.validationFailed([
    new ValidationResult(ValidationResultSeverity.Error, 'taken', ['name'], null),
]) as ICommandResult<unknown>;

describe('when publishing validation state and a validation is overtaken', given(a_command_form_reporting_validation_state, context => {
    let result: ReturnType<typeof render>;

    beforeEach(async () => {
        result = await context.renderForm();

        const overtaken = context.beginSilentValidation('client');
        const newest = context.beginSilentValidation('client');

        await context.act(() => {
            context.applySilentValidationResult(accepted(), newest);
            context.applySilentValidationResult(rejected(), overtaken);
        });
    });

    afterEach(() => result.unmount());

    it('should let the newest verdict decide', () => context.validation!.client.verdict.should.equal('valid'));

    it('should keep the newest verdict current', () => context.validation!.client.freshness.should.equal('current'));

    it('should not report anything as pending', () => context.validation!.client.pending.should.be.false);
}));

describe('when publishing validation state and a result describes values that have since changed', given(a_command_form_reporting_validation_state, context => {
    let result: ReturnType<typeof render>;

    beforeEach(async () => {
        result = await context.renderForm();

        const issue = context.beginSilentValidation('client');
        await context.act(() => context.setCommandValues({ name: 'Other' }));
        await context.act(() => { context.applySilentValidationResult(accepted(), issue); });
    });

    afterEach(() => result.unmount());

    it('should report the verdict as stale', () => context.validation!.client.freshness.should.equal('stale'));
}));
