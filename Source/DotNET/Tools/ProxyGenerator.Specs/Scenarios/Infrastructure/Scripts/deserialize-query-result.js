// Deserialize query result data using field metadata, with derived type support.
// This approach keeps raw primitive/special-type values (TimeSpan, Guid, etc.) so that
// JSON.stringify produces JSON that System.Text.Json can re-deserialize correctly on the C# side.
// For properties with derivatives (e.g. IShape → CircleShape/RectangleShape), the correct
// derived class is instantiated so that constructor.name reflects the real type.
try {
    var response = JSON.parse('{{ESCAPED_JSON}}');
    var Fundamentals = require('@cratis/fundamentals');
    var DerivedType = Fundamentals && Fundamentals.DerivedType;

    if (response.data && __query.modelType && globalThis.Fields) {

        function getDerivedTypeId(cls) {
            try {
                return DerivedType && DerivedType.get ? DerivedType.get(cls) : undefined;
            } catch(e) { return undefined; }
        }

        function resolveType(field, value) {
            if (value._derivedTypeId) {
                var id = value._derivedTypeId;
                var candidates = [];
                if (field.derivatives && field.derivatives.length > 0) {
                    candidates = candidates.concat(field.derivatives);
                }
                if (DerivedType && DerivedType.getDerivedTypesFor && field.type) {
                    candidates = candidates.concat(DerivedType.getDerivedTypesFor(field.type));
                }
                for (var i = 0; i < candidates.length; i++) {
                    var d = candidates[i];
                    if (d && getDerivedTypeId(d) === id) {
                        return d;
                    }
                }
            }
            return null;
        }

        function deserializeObject(data, type) {
            if (!data || !type || !globalThis.Fields) return data;

            var fields = globalThis.Fields.getFieldsForType(type);
            if (!fields || fields.length === 0) {
                // Type has no @field decorators (e.g. TimeSpan, Guid) — keep raw value
                return data;
            }

            var deserialized;
            try { deserialized = new type(); } catch(e) { deserialized = {}; }

            for (var i = 0; i < fields.length; i++) {
                var field = fields[i];
                if (!data.hasOwnProperty(field.name)) continue;
                var value = data[field.name];

                if (value === null || value === undefined) {
                    deserialized[field.name] = value;
                } else if (Array.isArray(value) && field.enumerable) {
                    deserialized[field.name] = value.map(function(item) {
                        if (typeof item !== 'object' || item === null) return item;
                        var itemType = resolveType(field, item) || field.type;
                        return itemType ? deserializeObject(item, itemType) : item;
                    });
                } else if (typeof value === 'object' && !Array.isArray(value)) {
                    var resolvedType = resolveType(field, value);
                    if (resolvedType) {
                        deserialized[field.name] = deserializeObject(value, resolvedType);
                    } else if (field.type) {
                        var nestedFields = globalThis.Fields.getFieldsForType(field.type);
                        if (nestedFields && nestedFields.length > 0) {
                            deserialized[field.name] = deserializeObject(value, field.type);
                        } else {
                            deserialized[field.name] = value;
                        }
                    } else {
                        deserialized[field.name] = value;
                    }
                } else {
                    deserialized[field.name] = value;
                }
            }
            return deserialized;
        }

        __queryResult.data = deserializeObject(response.data, __query.modelType);
    }
} catch(e) {
    // Ignore deserialization errors
}
