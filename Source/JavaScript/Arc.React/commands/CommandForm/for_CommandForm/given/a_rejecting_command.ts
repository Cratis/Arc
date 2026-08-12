// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Command, CommandValidator, CommandResult } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';

class RejectingCommandValidator extends CommandValidator<RejectingCommand> {
}

/**
 * A command whose execution rejects rather than answering with a failed result.
 *
 * Command.performRequest catches transport failures and returns CommandResult.failed, so a broken
 * network never rejects out of execute(). What still can is the command itself - a validator that
 * throws, a payload that cannot be built, or an override like this one. A form has no say in which
 * commands it is handed, so it has to survive the ones that reject.
 */
export class RejectingCommand extends Command<object> {
    readonly route = '/api/rejecting-command';
    readonly validation = new RejectingCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('name', String, true)
    ];

    name?: string;

    get requestParameters(): string[] {
        return [];
    }

    execute(): Promise<CommandResult<object>> {
        return Promise.reject(new Error('The command refused to execute'));
    }
}
