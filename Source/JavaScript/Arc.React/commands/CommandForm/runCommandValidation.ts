// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ICommandResult } from '@cratis/arc/commands';

export async function runCommandValidation(commandInstance: unknown, useServerValidation: boolean): Promise<ICommandResult<unknown> | undefined> {
    if (!commandInstance) {
        return undefined;
    }

    const instance = commandInstance as Record<string, unknown>;

    if (useServerValidation && typeof instance.validate === 'function') {
        return (instance.validate as () => Promise<ICommandResult<unknown>>)();
    }

    if (typeof instance.validateClientSide === 'function') {
        return (instance.validateClientSide as () => ICommandResult<unknown>)();
    }

    return undefined;
}
