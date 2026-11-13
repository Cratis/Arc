// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace OAuthPatCallerAcceptance.Specs.given;

public static class HostileMetadata
{
    public static IReadOnlyDictionary<string, string?> Actor => new Dictionary<string, string?>
    {
        ["x-cratis-act"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"iss":"https://actor.example.test","sub":"agent"}""")),
        ["x-cratis-grant-id"] = "delegation-grant"
    };

    public static IReadOnlyDictionary<string, string?> SenderBinding => new Dictionary<string, string?>
    {
        ["x-cratis-sender-binding"] = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"jkt\":\"" + new string('A', 43) + "\",\"jti\":\"proof-id\"}"))
    };
}
