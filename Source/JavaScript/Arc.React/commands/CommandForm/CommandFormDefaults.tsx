// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { createContext, useContext, useMemo } from 'react';
import type { CommandFormSettings } from './CommandFormSettings.js';

const DefaultsContext = createContext<CommandFormSettings>({});

/** Props for scoped command form defaults. */
export interface CommandFormDefaultsProps {
    value: CommandFormSettings;
    children?: React.ReactNode;
}

const inheritSettings = <TSettings extends CommandFormSettings>(parent: CommandFormSettings, settings: TSettings): TSettings => {
    const resolved = { ...parent, ...settings };
    for (const key of Object.keys(parent) as (keyof CommandFormSettings)[]) {
        if (settings[key] === undefined) {
            Object.assign(resolved, { [key]: parent[key] });
        }
    }
    return resolved;
};

/** Supplies defaults to descendant forms; defined inner settings override outer settings. */
export function CommandFormDefaults({ value, children }: CommandFormDefaultsProps) {
    const parent = useContext(DefaultsContext);
    const resolved = useMemo(() => inheritSettings(parent, value), [parent, value]);
    return <DefaultsContext.Provider value={resolved}>{children}</DefaultsContext.Provider>;
}

/** Resolves scoped defaults without altering non-settings properties or nested command values. */
export function useCommandFormDefaults<TSettings extends CommandFormSettings>(settings: TSettings): TSettings {
    const parent = useContext(DefaultsContext);
    return inheritSettings(parent, settings);
}
