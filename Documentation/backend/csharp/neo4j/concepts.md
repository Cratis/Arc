---
title: Use concepts with Neo4j in Cratis Arc
description: Bind ConceptAs values as Cypher parameters and get them back as concept-typed properties when a graph query returns rows.
---

Strongly typed values travel through the graph as the primitives they wrap, the same way they do in [MongoDB](../mongodb/concepts.md). You never unwrap a `ConceptAs<T>` to bind it or rebuild one from a row.

## Parameters

A `ConceptAs<T>` binds as its inner value, including inside lists and maps. `GraphParameterConverter` applies these rules to every parameter:

| Value | Bound as |
| --- | --- |
| A concept | Its inner value, converted by the rules below |
| `Guid` | Its string form, because the graph has no GUID type |
| An enum | Its name |
| `decimal` | `double`, because the graph has no decimal type |
| A list or a dictionary | A list or a map with every value converted |
| Anything else | Unchanged |

```csharp
await graph.Query<GroupMember>(cypher, new { groupId = new GroupId(id) });   // binds the Guid's text
```

This is what Chronicle's Neo4j sink stores, so an identity written from an event is found by the same parameter.

## Results

`GraphRecordMapper` builds the result type from the row. A concept property is created from the primitive the row holds, and:

- a `Guid` is read from its string form;
- an enum is read from its name, or its number;
- the graph's 64-bit integers are narrowed to `int` and the other numeric types the property asks for;
- a list becomes an array, `List<T>`, `IReadOnlyList<T>` or `IEnumerable<T>`, with every element converted, so a list of concepts maps to a list of concepts;
- a missing column leaves the property at its default.

The graph stores primitives and arrays of primitives only, so a property that holds an object cannot be written by a projection; return a node or a relationship from your query and let the mapper read its properties.
