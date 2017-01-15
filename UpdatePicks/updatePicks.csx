#load "..\Lib\PickEntity.csx"

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

public static async Task Run(TimerInfo timer, TraceWriter log)
{
    var start = DateTime.UtcNow;
    Dictionary<string,List<PickRecord>> data = new Dictionary<string, List<PickRecord>>();

    var season = await GetSeason();

    var tableService = new TableService("PicksStorage".GetEnvVar());
    var blobService = new BlobService("PicksStorage".GetEnvVar());

    var pastTournaments = await GetPastTournaments();
    foreach (JObject tournament in pastTournaments)
    {
        var pickPartitionKey = $"{season}:PGA TOUR:{tournament["Index"]}";
        List<PickEntity> partition = (await tableService.GetPartitionAsync<PickEntity>("picks", pickPartitionKey)).OrderBy(x => x.RowKey).ToList();
        foreach (var pickEntity in partition)
        {
            var userId = pickEntity.UserId;
            var userEmail = pickEntity.UserEmail;
            var playerId = pickEntity.PlayerId;
            var playerName = pickEntity.PlayerName;
            if (!data.ContainsKey(userId))
                data[userId] = new List<PickRecord>();
            data[userId].Add(new PickRecord
            {
                Season = season,
                Tour = pickEntity.Tour,
                TournamentIndex = pickEntity.TournamentIndex,
                TournamentName = (string)tournament["Name"],
                TournamentId = pickEntity.TournamentId,
                UserId = userId,
                UserEmail = userEmail,
                PlayerId = playerId,
                PlayerName = playerName,
            });
        }
    }
    foreach (var item in data)
    {
        var path = $"{season}/PGA TOUR/{item.Key}.json";
        var value = JArray.FromObject(item.Value).ToString(Formatting.Indented);
        await blobService.UploadBlobAsync("pickhistory", path, value);
    }
    var end = DateTime.UtcNow;
    log.Info($"Execution Time: {end - start}");
}

public static async Task<int> GetSeason()
{
    var serviceToken = "ServiceToken".GetEnvVar();
    var tournamentsUrl = "TournamentsUrl".GetEnvVar();

    var response = await RestService.AuthorizedPostAsync($"{tournamentsUrl}/api/Season", null, serviceToken);
    return (int)response["Season"];
}

public static async Task<JArray> GetPastTournaments()
{
    var serviceToken = "ServiceToken".GetEnvVar();
    var tournamentsUrl = "TournamentsUrl".GetEnvVar();

    var response = await RestService.AuthorizedPostAsync($"{tournamentsUrl}/api/GetTournaments", new Dictionary<string, string>
    {
        ["season"] = "current",
        ["tour"] = "PGA TOUR",
        ["key"] = "state",
        ["value"] = "dequeued",
    }, serviceToken);
    return (JArray)response;
}

public class PickRecord
{
    public int Season { get; set; }
    public string Tour { get; set; }
    public string TournamentIndex { get; set; }
    public string TournamentId { get; set; }
    public string TournamentName { get; set; }
    public string UserId { get; set; }
    public string UserEmail { get; set; }
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
}