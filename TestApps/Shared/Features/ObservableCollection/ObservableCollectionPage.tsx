// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useMemo } from 'react';
import { AddObservableCollectionItem } from './AddObservableCollectionItem';
import { All } from './ObservableCollectionItem';
import { RemoveObservableCollectionItem } from './RemoveObservableCollectionItem';

/**
 * Page for testing observable-query collection updates driven by backend add and remove commands.
 */
export const ObservableCollectionPage = () => {
    const [result] = All.use();
    const [addCommand, setAddValues] = AddObservableCollectionItem.use({ id: 3, label: '' });
    const [removeCommand, setRemoveValues] = RemoveObservableCollectionItem.use();

    const items = result.data ?? [];
    const nextId = useMemo(
        () => items.length > 0 ? Math.max(...items.map(item => item.id)) + 1 : 1,
        [items]
    );

    const handleAdd = async () => {
        await addCommand.execute();
        setAddValues({ id: nextId + 1, label: '' });
    };

    const handleRemove = async (id: number) => {
        setRemoveValues({ id });
        await removeCommand.execute();
    };

    return (
        <div>
            <h2>Observable Collection</h2>
            <p>
                Uses <code>useObservableQuery</code> against a backend collection and drives mutations with
                commands. In <strong>Delta</strong> transfer mode, adding or removing an item should update the
                list below without requiring a refresh.
            </p>

            <section style={{ border: '1px solid #ddd', borderRadius: 6, padding: 16, marginBottom: 16 }}>
                <h3 style={{ marginTop: 0 }}>Add item</h3>
                <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
                    <input
                        type="number"
                        value={addCommand.id ?? nextId}
                        onChange={event => setAddValues({ id: Number(event.target.value) })}
                        style={{ width: 80 }}
                    />
                    <input
                        value={addCommand.label ?? ''}
                        placeholder="Label"
                        onChange={event => setAddValues({ label: event.target.value })}
                        style={{ width: 180 }}
                    />
                    <button onClick={handleAdd} disabled={!addCommand.label?.trim()}>
                        Add
                    </button>
                </div>
            </section>

            <section style={{ border: '1px solid #ddd', borderRadius: 6, padding: 16 }}>
                <h3 style={{ marginTop: 0 }}>Current collection</h3>
                <p style={{ color: '#666', fontSize: 13 }}>
                    Item count: <strong>{items.length}</strong>
                </p>
                {result.isPerforming && <p>Connecting…</p>}
                {!result.isPerforming && items.length === 0 && <p>No items in the collection.</p>}
                {items.length > 0 && (
                    <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                        <thead>
                            <tr style={{ background: '#f0f0f0' }}>
                                <th style={{ padding: '6px 8px', textAlign: 'left' }}>ID</th>
                                <th style={{ padding: '6px 8px', textAlign: 'left' }}>Label</th>
                                <th style={{ padding: '6px 8px', textAlign: 'right' }}>Action</th>
                            </tr>
                        </thead>
                        <tbody>
                            {items.map(item => (
                                <tr key={item.id} style={{ borderBottom: '1px solid #eee' }}>
                                    <td style={{ padding: '6px 8px' }}>{item.id}</td>
                                    <td style={{ padding: '6px 8px' }}>{item.label}</td>
                                    <td style={{ padding: '6px 8px', textAlign: 'right' }}>
                                        <button onClick={() => handleRemove(item.id)}>Remove</button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                )}
            </section>
        </div>
    );
};