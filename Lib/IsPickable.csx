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

public static async Task<bool> IsPickable(int season, string tourName, string userId, string playerId)
{
    var blobService = new BlobService("PicksStorage".GetEnvVar());
    var picksJson = await blobService.DownloadBlobAsync("picks", $"{season}/{tourName}/{userId}.json");
    if (!string.IsNullOrEmpty(picksJson))
    {
        var jPicks = JArray.Parse(picksJson);
        var pickCount = 0;
        foreach (var jToken in jPicks)
        {
            var jObject = (JObject)jToken;
            var pickPlayerId = (string)jObject["PlayerId"];
            if (playerId.Equals(pickPlayerId, StringComparison.OrdinalIgnoreCase))
                pickCount++;
            if (pickCount >= 2)
                return false;
        }
    }
    return true;
}