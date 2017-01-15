#load "..\Lib\IsPickable.csx"

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

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    var jwt = await req.GetJwt("submitter");
    if(jwt == null)
        return req.CreateError(HttpStatusCode.Unauthorized);
    string userId = jwt.UserId;

    IDictionary<string, string> query = req.GetQueryNameValuePairs().ToDictionary(pair => pair.Key, pair => pair.Value);

    var season = Convert.ToInt32(query["season"]);
    var tour = query["tour"];
    var playerId = query["playerId"];
    var playerName = query["playerName"];
    var tournamentId = query["tournamentId"];
    var tournamentIndex = query["tournamentIndex"];

    var isPickable = await IsPickable(season, tour, userId, playerId);
    if (!isPickable)
        return req.CreateError(HttpStatusCode.BadRequest);
    return req.CreateOk(new { PlayerId = playerId });
}