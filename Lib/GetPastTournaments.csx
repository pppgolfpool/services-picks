#r "..\Common\PppPool.Common.dll"
#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PppPool.Common;

public static async Task<JArray> GetPastTournaments()
{
    var serviceToken = "ServiceToken".GetEnvVar();
    var tournamentsUrl = "TournamentsUrl".GetEnvVar();

    var response = await RestService.AuthorizedPostAsync($"{tournamentsUrl}/api/GetTournaments", new Dictionary<string, string>
    {
        ["season"] = "current",
        ["tour"] = "PGA TOUR",
        ["key"] = "state",
        ["value"] = "dequeued,progressing,completed",
    }, serviceToken);
    return (JArray)response;
}