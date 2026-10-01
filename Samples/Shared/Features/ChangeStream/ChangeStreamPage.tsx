// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';
import { All, ChangeStreamItem } from './ChangeStreamItem';
import { AddChangeStreamItem } from './AddChangeStreamItem';
import { UpdateChangeStreamItem } from './UpdateChangeStreamItem';
import { RemoveChangeStreamItem } from './RemoveChangeStreamItem';

interface ChangeLogEntry {
    added: ChangeStreamItem[];
    replaced: ChangeStreamItem[];
    removed: ChangeStreamItem[];
    timestamp: Date;
}

const entryColor = {
    added: '#d4edda',
    replaced: '#fff3cd',
    removed: '#f8d7da',
} as const;

const pillStyle = (color: string): React.CSSProperties => ({
    background: color,
    borderRadius: 4,
    padding: '1px 6px',
    fontSize: 12,
    display: 'inline-block',
    marginRight: 4,
});

/**
 * Page that demonstrates the ChangeStream feature with add, update, and remove operations.
 *
 * Shows the current collection alongside a rolling change log so you can observe
 * the difference between Delta mode (only what changed) and Full mode (everything).
 */
export const ChangeStreamPage = () => {
    const [allResult] = All.use();
    const changes = All.useChangeStream(item => item.id);
    const [changeLog, setChangeLog] = useState<ChangeLogEntry[]>([]);

    const [addCommand, setAddValues] = AddChangeStreamItem.use();
    const [updateCommand, setUpdateValues] = UpdateChangeStreamItem.use();
    const [removeCommand, setRemoveValues] = RemoveChangeStreamItem.use();

    const items = allResult.data ?? [];
    const nextId = items.length > 0 ? Math.max(...items.map(i => i.id)) + 1 : 1;

    useEffect(() => {
        const hasChanges = changes.added.length > 0 || changes.replaced.length > 0 || changes.removed.length > 0;
        if (!hasChanges) return;
        setChangeLog(log => [
            { added: changes.added, replaced: changes.replaced, removed: changes.removed, timestamp: new Date() },
            ...log.slice(0, 19),
        ]);
    }, [changes]);

    const handleAdd = async () => {
        await addCommand.execute();
        setAddValues({ id: nextId + 1, label: '', value: 0 });
    };

    const handleUpdate = async () => {
        await updateCommand.execute();
    };

    const handleRemove = async (id: number) => {
        setRemoveValues({ id });
        await removeCommand.execute();
    };

    return (
        <div>
            <h2>Change Stream</h2>
            <p>
                Demonstrates <code>useChangeStream</code> with add, update, and remove operations.
                Switch the <strong>Transfer mode</strong> in the toolbar to compare{' '}
                <strong>Delta</strong> (only changed items) vs <strong>Full</strong> (all items on every update).
            </p>

            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 24 }}>

                {/* Left: current state + mutation forms */}
                <div>
                    <h3>Current collection</h3>
                    {allResult.isPerforming && <p>Connecting…</p>}
                    <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
                        <thead>
                            <tr style={{ background: '#f0f0f0' }}>
                                <th style={{ padding: '4px 8px', textAlign: 'left' }}>ID</th>
                                <th style={{ padding: '4px 8px', textAlign: 'left' }}>Label</th>
                                <th style={{ padding: '4px 8px', textAlign: 'right' }}>Value</th>
                                <th />
                            </tr>
                        </thead>
                        <tbody>
                            {items.map(item => (
                                <tr key={item.id} style={{ borderBottom: '1px solid #eee' }}>
                                    <td style={{ padding: '4px 8px' }}>{item.id}</td>
                                    <td style={{ padding: '4px 8px' }}>{item.label}</td>
                                    <td style={{ padding: '4px 8px', textAlign: 'right' }}>{item.value}</td>
                                    <td style={{ padding: '4px 8px', textAlign: 'right' }}>
                                        <button
                                            style={{ marginRight: 4, fontSize: 12 }}
                                            onClick={() => setUpdateValues({ id: item.id, label: item.label, value: item.value })}
                                        >
                                            Edit
                                        </button>
                                        <button
                                            style={{ fontSize: 12, color: 'red' }}
                                            onClick={() => handleRemove(item.id)}
                                        >
                                            Remove
                                        </button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>

                    <h3 style={{ marginTop: 24 }}>Add item</h3>
                    <div style={{ display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}>
                        <input
                            type="number"
                            placeholder="ID"
                            value={addCommand.id ?? nextId}
                            onChange={e => setAddValues({ id: Number(e.target.value) })}
                            style={{ width: 60 }}
                        />
                        <input
                            placeholder="Label"
                            value={addCommand.label ?? ''}
                            onChange={e => setAddValues({ label: e.target.value })}
                            style={{ width: 100 }}
                        />
                        <input
                            type="number"
                            placeholder="Value"
                            value={addCommand.value ?? 0}
                            onChange={e => setAddValues({ value: Number(e.target.value) })}
                            style={{ width: 70 }}
                        />
                        <button onClick={handleAdd}>Add</button>
                    </div>

                    <h3 style={{ marginTop: 24 }}>Update item</h3>
                    <div style={{ display: 'flex', gap: 6, alignItems: 'center', flexWrap: 'wrap' }}>
                        <select
                            value={updateCommand.id ?? ''}
                            onChange={e => {
                                const id = Number(e.target.value);
                                const existing = items.find(i => i.id === id);
                                setUpdateValues({ id, label: existing?.label ?? '', value: existing?.value ?? 0 });
                            }}
                            style={{ width: 80 }}
                        >
                            <option value="">— ID —</option>
                            {items.map(i => <option key={i.id} value={i.id}>{i.id}</option>)}
                        </select>
                        <input
                            placeholder="Label"
                            value={updateCommand.label ?? ''}
                            onChange={e => setUpdateValues({ label: e.target.value })}
                            style={{ width: 100 }}
                        />
                        <input
                            type="number"
                            placeholder="Value"
                            value={updateCommand.value ?? 0}
                            onChange={e => setUpdateValues({ value: Number(e.target.value) })}
                            style={{ width: 70 }}
                        />
                        <button onClick={handleUpdate} disabled={!updateCommand.id}>Update</button>
                    </div>
                </div>

                {/* Right: change log */}
                <div>
                    <h3>Change log <span style={{ fontWeight: 'normal', fontSize: 13 }}>(last 20 updates)</span></h3>
                    <p style={{ fontSize: 12, color: '#666' }}>
                        Each row is one server push. In <strong>Delta</strong> mode only mutated items appear.
                        In <strong>Full</strong> mode every push shows the entire collection as{' '}
                        <span style={pillStyle(entryColor.added)}>added</span>.
                    </p>
                    {changeLog.length === 0 && <p style={{ color: '#888' }}>No updates yet — mutate the collection on the left.</p>}
                    <div style={{ maxHeight: 480, overflowY: 'auto' }}>
                        {changeLog.map((entry, index) => (
                            <div
                                key={index}
                                style={{ marginBottom: 8, padding: '6px 10px', border: '1px solid #ddd', borderRadius: 4, fontSize: 12 }}
                            >
                                <div style={{ color: '#888', marginBottom: 4 }}>
                                    {entry.timestamp.toLocaleTimeString()}
                                    {' — '}
                                    {entry.added.length > 0 && <span style={pillStyle(entryColor.added)}>+{entry.added.length} added</span>}
                                    {entry.replaced.length > 0 && <span style={pillStyle(entryColor.replaced)}>~{entry.replaced.length} replaced</span>}
                                    {entry.removed.length > 0 && <span style={pillStyle(entryColor.removed)}>−{entry.removed.length} removed</span>}
                                </div>
                                {entry.added.map(i => (
                                    <div key={`a-${i.id}`} style={{ background: entryColor.added, borderRadius: 3, padding: '2px 6px', marginBottom: 2 }}>
                                        + #{i.id} {i.label} = {i.value}
                                    </div>
                                ))}
                                {entry.replaced.map(i => (
                                    <div key={`r-${i.id}`} style={{ background: entryColor.replaced, borderRadius: 3, padding: '2px 6px', marginBottom: 2 }}>
                                        ~ #{i.id} {i.label} = {i.value}
                                    </div>
                                ))}
                                {entry.removed.map(i => (
                                    <div key={`d-${i.id}`} style={{ background: entryColor.removed, borderRadius: 3, padding: '2px 6px', marginBottom: 2 }}>
                                        − #{i.id} {i.label} = {i.value}
                                    </div>
                                ))}
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </div>
    );
};
