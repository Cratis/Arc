// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Guid } from '@cratis/fundamentals';
import { AddObservableCollectionWithGuidItem } from './AddObservableCollectionWithGuidItem';
import { All } from './ObservableCollectionWithGuidItem';
import { RemoveObservableCollectionWithGuidItem } from './RemoveObservableCollectionWithGuidItem';

/**
 * Page for testing observable-query collection updates with Guid identifiers.
 */
export const ObservableCollectionWithGuidPage = () => {
    const [result] = All.use();
    const [addCommand, setAddValues] = AddObservableCollectionWithGuidItem.use({ id: Guid.create(), label: '' });
    const [removeCommand, setRemoveValues] = RemoveObservableCollectionWithGuidItem.use();

    const items = result.data ?? [];

    const handleAdd = async () => {
        await addCommand.execute();
        setAddValues({ id: Guid.create(), label: '' });
    };

    const handleRemove = async (id: Guid) => {
        setRemoveValues({ id });
        await removeCommand.execute();
    };

    return (
        <div>
            <h2>Observable Collection With Guid</h2>
            <p>
                Uses <code>useObservableQuery</code> against a backend collection with <code>Guid</code> identifiers
                and drives mutations with commands. In <strong>Delta</strong> transfer mode, adding or removing an
                item should update the list below without requiring a refresh.
            </p>

            <section style={{ border: '1px solid #ddd', borderRadius: 6, padding: 16, marginBottom: 16 }}>
                <h3 style={{ marginTop: 0 }}>Add item</h3>
                <div style={{ display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
                    <input
                        value={addCommand.id?.toString() ?? ''}
                        readOnly
                        style={{ width: 280 }}
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
                                <tr key={item.id.toString()} style={{ borderBottom: '1px solid #eee' }}>
                                    <td style={{ padding: '6px 8px' }}>{item.id.toString()}</td>
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