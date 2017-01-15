#load "..\Lib\IsPickable.csx"
#load "..\Lib\PickEntity.csx"

#r "..\Common\PppPool.Common.dll"
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

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    var jwt = await req.GetJwt("submitter");
    if (jwt == null)
        return req.CreateError(HttpStatusCode.Unauthorized);
    string userId = jwt.UserId;
    string email = jwt.Email;

    IDictionary<string, string> query = req.GetQueryNameValuePairs().ToDictionary(pair => pair.Key, pair => pair.Value);

    var tour = query["tour"];
    var playerId = query["playerId"];
    var playerName = query["playerName"];

    var tournamentsUrl = "TournamentsUrl".GetEnvVar();
    var pickingTournaments = await RestService.AuthorizedPostAsync($"{tournamentsUrl}/api/GetTournaments", new Dictionary<string, string>
    {
        ["season"] = "current",
        ["tour"] = tour,
        ["key"] = "state",
        ["value"] = "picking",
    }, "ServiceToken".GetEnvVar());

    if (((JArray)pickingTournaments).Count != 1)
        return req.CreateError(HttpStatusCode.BadRequest);

    var tournament = ((JArray)pickingTournaments)[0];

    var season = (int)tournament["Season"];

    var isPickable = await IsPickable(season, tour, userId, playerId);
    if (!isPickable)
        return req.CreateError(HttpStatusCode.BadRequest);

    var pickEntity = new PickEntity(season, tour, userId, email, playerId, playerName, (string)tournament["PermanentNumber"], (string)tournament["Index"]);
    var tableService = new TableService("PicksStorage".GetEnvVar());
    await tableService.UpsertEntityAsync("picks", pickEntity);

    return req.CreateOk(new { PlayerId = playerId });
}