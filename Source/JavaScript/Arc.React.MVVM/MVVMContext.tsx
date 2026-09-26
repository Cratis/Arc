// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { Bindings } from './Bindings.js';
import { configure as configureMobx } from 'mobx';
import { MobxOptions } from './MobxOptions.js';
import { deferredReactionScheduler } from './deferredReactionScheduler.js';

export interface MVVMProps {
    children?: JSX.Element | JSX.Element[];
    mobx?: MobxOptions;
}

export const MVVMContext = React.createContext({});

export const MVVM = (props: MVVMProps) => {
    const options: MobxOptions = {
        enforceActions: 'never',
        reactionScheduler: deferredReactionScheduler,
        ...(props.mobx || {}),
    };

    configureMobx(options);

    Bindings.initialize();

    return <MVVMContext.Provider value={{}}>{props.children}</MVVMContext.Provider>;
};
