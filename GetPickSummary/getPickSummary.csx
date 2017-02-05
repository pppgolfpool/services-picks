#load "..\Lib\PickEntity.csx"
#load "..\Lib\GetSeason.csx"
#load "..\Lib\GetPastTournaments.csx"

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

    IDictionary<string, string> query = req.GetQueryNameValuePairs().ToDictionary(pair => pair.Key, pair => pair.Value);

    var season = 0;
    if (query["season"].ToLower() == "current")
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

    var blobService = new BlobService("PicksStorage".GetEnvVar());
    var blockText = await blobService.DownloadBlobAsync("pickhistory", $"{season}/{tour}/summary.json");
    var jobject = JObject.Parse(blockText);

    return req.CreateOk(jobject);
}