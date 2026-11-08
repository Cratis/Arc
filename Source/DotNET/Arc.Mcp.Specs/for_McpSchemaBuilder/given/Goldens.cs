// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Mcp.for_McpSchemaBuilder.given;

public static class Goldens
{
    const string Address = "{'type':'object','properties':{'street':{'type':'string'},'line2':{'type':['string','null']}},'required':['street']}";

    public static readonly string Command = (
        "{'type':'object','properties':{" +
        "'id':{'type':'string','format':'uuid'}," +
        "'name':{'type':'string'}," +
        "'priority':{'type':'string','enum':['Low','High']}," +
        "'address':" + Address + "," +
        "'age':{'type':['integer','null']}," +
        "'tags':{'type':'array','items':{'type':'string'}}," +
        "'previous':{'type':'array','items':" + Address + "}," +
        "'idempotency_key':{'type':'string','description':'Optional key that correlates retries of the same request.'}}," +
        "'required':['id','name','priority','address','tags','previous']}").Replace('\'', '"');

    public static readonly string PagedQuery = (
        "{'type':'object','properties':{" +
        "'customerId':{'type':'string','format':'uuid'}," +
        "'priority':{'type':'string','enum':['Low','High']}," +
        "'name':{'type':'string'}," +
        "'page':{'type':'integer','minimum':0}," +
        "'page_size':{'type':'integer','minimum':1}}," +
        "'required':['customerId'],'additionalProperties':false}").Replace('\'', '"');
}
