// Deserialize query result data using type fields
try {
    var response = JSON.parse('{{ESCAPED_JSON}}');
    
    if (response.data && __query.modelType && globalThis.Fields) {
        // Helper function to deserialize an object using its type's fields
        function deserializeObject(data, type) {
            if (!data || !type || !globalThis.Fields) return data;
            
            var fields = globalThis.Fields.getFieldsForType(type);
            if (!fields || fields.length === 0) {
                return data;
            }
            
            var deserialized = {};
            for (var i = 0; i < fields.length; i++) {
                var field = fields[i];
                if (data.hasOwnProperty(field.name)) {
                    var value = data[field.name];
                    
                    // Handle null/undefined
                    if (value === null || value === undefined) {
                        deserialized[field.name] = value;
                    }
                    // Handle arrays
                    else if (Array.isArray(value) && field.type && globalThis[field.type.name]) {
                        deserialized[field.name] = value.map(item => 
                            typeof item === 'object' && item !== null 
                                ? deserializeObject(item, field.type) 
                                : item
                        );
                    }
                    // Handle nested objects
                    else if (typeof value === 'object' && !Array.isArray(value) && field.type && globalThis[field.type.name]) {
                        deserialized[field.name] = deserializeObject(value, field.type);
                    }
                    // Handle primitives
                    else {
                        deserialized[field.name] = value;
                    }
                }
            }
            return deserialized;
        }
        
        __queryResult.data = deserializeObject(response.data, __query.modelType);
    }
} catch(e) {
    // Ignore deserialization errors
}
