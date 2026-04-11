// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import 'reflect-metadata';
import * as browser from './browser';
import * as messaging from './messaging';
import * as dialogs from './dialogs';
export * from './Bindings';
export * from './MVVMContext';
export * from './withViewModel';
export * from './IViewModelDetached';
export * from './WellKnownBindings';
export * from './IHandleProps';
export * from './IHandleParams';
export * from './IHandleQueryParams';
export * from './params';
export * from './queryParams';
export * from './props';

export {
    browser,
    messaging,
    dialogs
};
