#load "..\Lib\PickEntity.csx"

#r "..\Common\PppPool.Common.dll"
#r "..\Common\Microsoft.WindowsAzure.Storage.dll"
#r "System.Xml.Linq"
#r "Newtonsoft.Json"

using System;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using PppPool.Common;

// season, tour, tournament
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    var jwt = await req.GetJwt("submitter");
    if (jwt == null)
        return req.CreateError(HttpStatusCode.Unauthorized); 

    IDictionary<string, string> query = req.GetQueryNameValuePairs().ToDictionary(pair => pair.Key, pair => pair.Value);

    var season = 0;
    if(query["season"].ToLower() == "current")
    {
        var tournamentsUrl = "TournamentsUrl".GetEnvVar();
        var seasonData = await RestService.AuthorizedPostAsync($"{tournamentsUrl}/api/Season", null, "ServiceToken".GetEnvVar());
        season = (int)seasonData["Season"];
    }
    else
    {
        season = Convert.ToInt32(query["season"]);
    }

    var tour = query["tour"];
    var index = query["tournamentIndex"];

    var connectionString = "PicksStorage".GetEnvVar();
    var tableService = new TableService(connectionString);
    var picks = await tableService.GetPartitionAsync<PickEntity>("picks", $"{season}:{tour}:{index}");

    if (query.ContainsKey("user"))
    {
        var userId = jwt.UserId;
        var pickEntity = picks.FirstOrDefault(x => x.UserId == userId);
        if (pickEntity == null)
            return req.CreateOk(new { empty = true });
        JObject pick = JObject.FromObject(picks.FirstOrDefault(x => x.UserId == userId));
        if (pick != null)
            pick["UserEmail"] = jwt.Email;
        return pick != null ? req.CreateOk(pick) : req.CreateOk(new { empty = true });
    }
    else
    {

        var usersUrl = "UserUrl".GetEnvVar();
        var profiles = await RestService.AuthorizedPostAsync($"{usersUrl}/api/GetProfile", new Dictionary<string, string>
        {
            ["key"] = "all",
        }, "ServiceToken".GetEnvVar());

        var picksList = new List<JObject>();
        foreach (var pick in picks)
        {
            var profile = profiles.FirstOrDefault(x => (string)x["UserId"] == pick.UserId);
            var jPick = JObject.FromObject(pick);
            jPick.Property("ETag").Remove();
            jPick["Email"] = profile != null ? (string)profile["Email"] : null;
            jPick["Poolie"] = profile != null ? (string)profile["Name"] : null;
            picksList.Add(jPick);
        }

        return req.CreateOk(picksList);
    }
}