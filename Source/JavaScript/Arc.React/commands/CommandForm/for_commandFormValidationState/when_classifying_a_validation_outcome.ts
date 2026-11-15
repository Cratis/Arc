// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CommandResult, type ICommandResult } from '@cratis/arc/commands';
import { ValidationResult, ValidationResultReason, ValidationResultSeverity } from '@cratis/arc/validation';
import { classifyValidationOutcome, CommandFormValidationNotRunReason } from '../commandFormValidationState.js';

const failing = (reason?: string): ICommandResult<unknown> =>
    CommandResult.validationFailed([
        new ValidationResult(ValidationResultSeverity.Error, 'message', ['name'], null, reason),
    ]) as ICommandResult<unknown>;

const missingHandler = (): ICommandResult<unknown> =>
    ({ ...CommandResult.failed(['arbitrary diagnostic']), isMissingHandler: true } as ICommandResult<unknown>);

const unauthorized = (): ICommandResult<unknown> =>
    ({ ...CommandResult.empty, isAuthorized: false, isSuccess: false } as ICommandResult<unknown>);

describe('when classifying a validation outcome', () => {
    it('should treat an attempt that threw as an error', () =>
        classifyValidationOutcome(undefined).verdict.should.equal('error'));

    it('should treat an accepted result as valid', () =>
        classifyValidationOutcome(CommandResult.empty as ICommandResult<unknown>).verdict.should.equal('valid'));

    it('should treat a rule rejection as invalid', () =>
        classifyValidationOutcome(failing(ValidationResultReason.Rule)).verdict.should.equal('invalid'));

    it('should treat a rejection with an unknown reason as invalid', () =>
        classifyValidationOutcome(failing('somethingNew')).verdict.should.equal('invalid'));

    it('should treat a failed validator as an error', () =>
        classifyValidationOutcome(failing(ValidationResultReason.ValidatorFailed)).verdict.should.equal('error'));

    it('should treat an unavailable dependency as an error', () =>
        classifyValidationOutcome(failing(ValidationResultReason.DependencyUnavailable)).verdict.should.equal('error'));

    it('should treat a malformed request as an error', () =>
        classifyValidationOutcome(failing(ValidationResultReason.MalformedRequest)).verdict.should.equal('error'));

    it('should treat a framework failure as an error', () =>
        classifyValidationOutcome(failing(ValidationResultReason.FrameworkFailure)).verdict.should.equal('error'));

    it('should treat exceptions as an error', () =>
        classifyValidationOutcome(CommandResult.failed(['boom']) as ICommandResult<unknown>).verdict.should.equal('error'));

    it('should treat an unauthorized result as unknown', () =>
        classifyValidationOutcome(unauthorized()).verdict.should.equal('unknown'));

    it('should say why an unauthorized result did not run', () =>
        classifyValidationOutcome(unauthorized()).notRunReason!.should.equal(CommandFormValidationNotRunReason.Unauthorized));

    it('should treat a missing handler with exceptions as unknown', () =>
        classifyValidationOutcome(missingHandler()).verdict.should.equal('unknown'));

    it('should explain why a missing handler could not validate', () =>
        classifyValidationOutcome(missingHandler()).notRunReason!.should.equal(CommandFormValidationNotRunReason.MissingHandler));

    it('should preserve authorization precedence over a missing handler', () =>
        classifyValidationOutcome({ ...missingHandler(), isAuthorized: false }).notRunReason!.should.equal(CommandFormValidationNotRunReason.Unauthorized));

    it('should not infer a missing handler from exception text', () =>
        classifyValidationOutcome(CommandResult.failed(['No handler found for command of type Example']) as ICommandResult<unknown>).verdict.should.equal('error'));

    it('should not carry a not-run reason for a verdict', () =>
        (classifyValidationOutcome(failing()).notRunReason === undefined).should.be.true);
});
