// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { CommandResults } from '@cratis/arc/commands';
import { ICommandScope } from './ICommandScope.js';

/* eslint-disable @typescript-eslint/no-empty-function */
export const defaultCommandScopeContext: ICommandScope = new class extends ICommandScope {
    get parent() { return undefined; }
    get hasChanges() { return false; }
    get isPerforming() { return false; }
    get hasValidationFailures() { return false; }
    get hasExceptions() { return false; }
    get validationFailures() { return new Map(); }
    get aggregatedValidationFailures() { return []; }
    get exceptions() { return new Map(); }
    get aggregatedExceptions() { return []; }
    addCommand() { }
    addQuery() { }
    addChildScope() { }
    async execute() { return new CommandResults(new Map()); }
    revertChanges() { }
}();
/* eslint-enable @typescript-eslint/no-empty-function */

export const CommandScopeContext = React.createContext<ICommandScope>(defaultCommandScopeContext);
