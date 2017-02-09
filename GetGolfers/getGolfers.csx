#load "..\Lib\IsPickable.csx"

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

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    var jwt = await req.GetJwt("submitter");
    if (jwt == null)
        return req.CreateError(HttpStatusCode.Unauthorized);
    string authToken = jwt.AuthToken;
    string userId = jwt.UserId;

    // If this is being asked by and admin, then it could be for an "EmergencyPick". If a userId is supplied with the
    // request, use that userId, instead of the one making the pick.
    var adminJwt = await req.GetJwt("admin");
    if(adminJwt != null)
    {
        IDictionary<string, string> query = req.GetQueryNameValuePairs().ToDictionary(pair => pair.Key, pair => pair.Value);
        if (query.ContainsKey("userId"))
            userId = query["userId"];
    }

    var tournaments = await GetPickingTournament(authToken);
    if (tournaments == null || tournaments.Count == 0)
        return req.CreateError(HttpStatusCode.BadRequest);

    var tournament = (JObject)tournaments[0];

    var connectionString = "PicksStorage".GetEnvVar();
    JObject field = await RefreshFileService.RefreshJsonFile(connectionString, "data", $"r/{tournament["PermanentNumber"]}/field.json", TimeSpan.FromHours(1));

    var blobService = new BlobService("PicksStorage".GetEnvVar());
    var picksJson = await blobService.DownloadBlobAsync("pickhistory", $"{tournament["Season"]}/{tournament["Tour"]}/{userId}.json");
    if (string.IsNullOrEmpty(picksJson))
        picksJson = "[]";

    var jPicks = JArray.Parse(picksJson);
    
    List<JObject> players = new List<JObject>();
    foreach (JObject player in (JArray)field["Tournament"]["Players"])
    {
        var pickCount = 0;
        foreach (var jToken in jPicks)
        {
            var jObject = (JObject)jToken;
            var pickPlayerId = (string)jObject["PlayerId"];
            if (((string)player["TournamentPlayerId"]).Equals(pickPlayerId, StringComparison.OrdinalIgnoreCase))
                pickCount++;
        }
        if (pickCount < 2 && ((string)player["isAlternate"]).ToLower() == "no")
            players.Add(player);
    }

    return req.CreateOk(new
    {
        Tournament = new
        {
            Name = (string)tournament["Name"],
            Start = (DateTime)tournament["Start"],
            State = (string)tournament["State"],
            Index = (string)tournament["Index"],
        },
        Golfers = players,
    });

}

public static async Task<JArray> GetPickingTournament(string authToken)
{
    var tournamentsUrl = "TournamentsUrl".GetEnvVar();
    return (JArray) await RestService.AuthorizedPostAsync(tournamentsUrl + "/api/GetTournaments", new Dictionary<string, string>
    {
        ["season"] = "current",
        ["tour"] = "PGA TOUR",
        ["key"] = "state",
        ["value"] = "picking,progressing",
    }, authToken);
}