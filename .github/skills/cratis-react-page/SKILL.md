---
name: cratis-react-page
description: Step-by-step guidance for building a React page in a Cratis Arc application — listing data with DataPage, toolbar actions with CommandDialog, row selection, detail panels, observable queries, and MVVM. Use whenever building or modifying a React page that lists or displays data, adding a table or grid, wiring up Add/Edit/Delete actions, using DataPage, DataTableForQuery, CommandDialog, or any @cratis/components. Also trigger when connecting a React component to a proxy-generated query or observable query.
---

## Workflow

### Step 1 — Prerequisites

- Backend query and command endpoints must already exist (see `cratis-readmodel` and `cratis-command` skills)
- Run `dotnet build` on the backend to regenerate proxies
- All proxy `.ts` files in `<CratisProxiesOutputPath>` must be committed/saved before importing

Imports you will need:

```tsx
import { DataPage, MenuItemGroup, MenuItem } from '@cratis/components';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { useDialog, DialogProps } from '@cratis/arc.react/dialogs';
```

---

### Step 2 — Basic DataPage setup

`DataPage` combines a toolbar/menu, a data table, and an optional detail panel into one component.

```tsx
import { DataPage, Column } from '@cratis/components';
import { AllAccounts } from './queries/AllAccounts';

export const AccountsPage = () => {
    return (
        <DataPage
            query={AllAccounts}
            columns={[
                { header: 'Name', field: 'name' },
                { header: 'Balance', field: 'balance' },
            ]}
        />
    );
};
```

---

### Step 3 — Add menu actions

Menu items appear in the toolbar above the table. Create a separate dialog component using `DialogProps`, then wire it up with `useDialog`.

**Dialog component (`CreateAccountDialog.tsx`):**

```tsx
import { DialogProps } from '@cratis/arc.react/dialogs';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { CreateAccount } from './commands/CreateAccount';

export const CreateAccountDialog = ({ closeDialog }: DialogProps) => {
    return (
        <CommandDialog<CreateAccount>
            command={CreateAccount}
            title="Create Account"
            okLabel="Create"
        >
            <InputTextField<CreateAccount> value={c => c.name} label="Account Name" />
        </CommandDialog>
    );
};
```

**Page component:**

```tsx
import { DataPage, MenuItemGroup, MenuItem } from '@cratis/components';
import { useDialog } from '@cratis/arc.react/dialogs';
import { CreateAccountDialog } from './CreateAccountDialog';

export const AccountsPage = () => {
    const [CreateAccountWrapper, showCreateAccount] = useDialog(CreateAccountDialog);

    return (
        <>
            <DataPage
                query={AllAccounts}
                columns={[
                    { header: 'Name', field: 'name' },
                ]}
                menuItems={
                    <MenuItemGroup>
                        <MenuItem label="Add Account" onClick={() => showCreateAccount()} />
                    </MenuItemGroup>
                }
            />
            <CreateAccountWrapper />
        </>
    );
};
```

See `references/dialogs.md` for the full dialog pattern guide.

---

### Step 4 — Row selection and edit dialog

Pass a selected-row handler and an edit dialog. Supply the row data as props to the dialog component.

**Edit dialog component (`EditAccountDialog.tsx`):**

```tsx
import { DialogProps, DialogResult } from '@cratis/arc.react/dialogs';
import { CommandDialog } from '@cratis/components/CommandDialog';
import { InputTextField } from '@cratis/components/CommandForm';
import { EditAccount } from './commands/EditAccount';

interface EditAccountDialogProps extends DialogProps {
    accountId: string;
    name: string;
}

export const EditAccountDialog = ({ closeDialog, accountId, name }: EditAccountDialogProps) => {
    return (
        <CommandDialog<EditAccount>
            command={EditAccount}
            title="Edit Account"
            okLabel="Save"
            initialValues={{ accountId }}
            currentValues={{ name }}
        >
            <InputTextField<EditAccount> value={c => c.name} label="Account Name" />
        </CommandDialog>
    );
};
```

**Page component:**

```tsx
const [EditAccountWrapper, showEditAccount] = useDialog(EditAccountDialog);

<DataPage
    ...
    onRowSelected={(row) => showEditAccount({ accountId: row.id, name: row.name })}
/>

<EditAccountWrapper />
```

---

### Step 5 — Choose observable vs standard query

Use an **observable query** when the data updates in real time without user-triggered refresh:

```tsx
<DataPage
    observableQuery={AllAccountsLive}  // use observableQuery instead of query
    ...
/>
```

Observable query results push updates automatically. You cannot call `requery()` on observable queries.

Use a **standard query** for data that only changes when the user takes an action (and use `useEffect`/`refresh` to invalidate after a command).

---

### Step 5b — Paging

Paging is automatic when the backend query returns `IQueryable<T>`. The generated TypeScript proxy includes `useWithPaging` and `useSuspenseWithPaging` methods that send `page` and `pageSize` query string parameters to the backend, where the query pipeline applies `.Skip()` and `.Take()` at the database level.

**Using the proxy directly (without DataPage):**

```tsx
const pageSize = 25;
const [result, perform, setSorting, setPage, setPageSize] = AllAccounts.useWithPaging(pageSize);

return (
    <>
        <DataTable value={result.data}>
            <Column field="name" header="Name" />
            <Column field="balance" header="Balance" />
        </DataTable>
        <Paginator
            first={result.paging.page * result.paging.size}
            rows={result.paging.size}
            totalRecords={result.paging.totalItems}
            onPageChange={(e) => setPage(e.page)}
        />
    </>
);
```

**Paging metadata** is available on `result.paging`:

| Property | Type | Description |
| -------- | ---- | ----------- |
| `page` | `number` | Current zero-based page number |
| `size` | `number` | Items per page |
| `totalItems` | `number` | Total items across all pages |
| `totalPages` | `number` | Total number of pages |

**Hook variants with paging:**

| Hook | Usage |
| ---- | ----- |
| `MyQuery.useWithPaging(pageSize)` | Standard query with paging |
| `MyQuery.useSuspenseWithPaging(pageSize)` | Suspense-compatible with paging |
| `MyObservableQuery.useWithPaging(pageSize)` | Observable query with paging |

All paging hooks return `setPage` and `setPageSize` callbacks. Calling either re-runs the query with updated parameters.

> **Important**: Paging only works when the backend returns `IQueryable<T>`. If the backend returns `IEnumerable<T>` or `List<T>`, paging parameters are ignored and all rows are returned.

---

### Step 6 — Detail panel (optional)

A detail panel renders beside the table when a row is selected.

```tsx
<DataPage
    query={AllAccounts}
    detailPanel={(row) => <AccountDetail account={row} />}
    ...
/>
```

---

### Step 7 — MVVM view model (optional, for complex pages)

For pages with complex state or coordination logic, wrap the page in a view model:

```tsx
import { withViewModel } from '@cratis/arc.react.mvvm';
import { injectable } from 'tsyringe';

@injectable()
class AccountsViewModel {
    selectedAccount?: AccountSummary;
}

export const AccountsPage = withViewModel(AccountsViewModel, ({ viewModel }) => (
    <DataPage ... />
));
```

See `references/mvvm.md` for full MVVM guidance.

---

## Quick decision guide

| Need | Use |
| --- | --- |
| Read-only list | `DataPage` with `query` |
| Real-time updates | `DataPage` with `observableQuery` |
| Add / create action | `MenuItem` + `CommandDialog` + `useDialog` |
| Edit selected row | Row selection + `CommandDialog` + `currentValues`/`initialValues` |
| Side detail panel | `detailPanel` prop on `DataPage` |
| Complex page logic | `withViewModel` MVVM wrapper |

## Reference files

- `references/data-page.md` — DataPage props, MenuItems, Columns, detailPanel
- `references/data-table.md` — Standalone DataTableForQuery / DataTableForObservableQuery
- `references/dialogs.md` — `useDialog`, `DialogProps`, `CommandDialog` full API
- `references/mvvm.md` — `withViewModel`, `@injectable`, `IHandleProps`, reactive props
