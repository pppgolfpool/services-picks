#load "..\Lib\PickEntity.csx"
#load "..\Lib\GetSeason.csx"
#load "..\Lib\GetPastTournaments.csx"

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

public static async Task Run(TimerInfo timer, TraceWriter log)
{
    var start = DateTime.UtcNow;
    Dictionary<string,List<PickRecord>> data = new Dictionary<string, List<PickRecord>>();

    var season = await GetSeason();

    var tableService = new TableService("PicksStorage".GetEnvVar());
    var blobService = new BlobService("PicksStorage".GetEnvVar());

    var summary = new JObject();

    var pastTournaments = await GetPastTournaments();
    var usersUrl = "UserUrl".GetEnvVar();
    var profiles = await RestService.AuthorizedPostAsync($"{usersUrl}/api/GetProfile", new Dictionary<string, string>
    {
        ["key"] = "all",
    }, "ServiceToken".GetEnvVar());

    foreach (JObject tournament in pastTournaments)
    {
        var pickPartitionKey = $"{season}:PGA TOUR:{tournament["Index"]}";
        List<PickEntity> partition = (await tableService.GetPartitionAsync<PickEntity>("picks", pickPartitionKey)).OrderBy(x => x.RowKey).ToList();
        foreach (var pickEntity in partition)
        {
            var userId = pickEntity.UserId;
            var playerId = pickEntity.PlayerId;
            var playerName = pickEntity.PlayerName;
            if(pickEntity.TournamentId.Length == 2)
            {
                pickEntity.TournamentId = "0" + pickEntity.TournamentId;
            }
            if(pickEntity.TournamentId.Length == 1)
            {
                pickEntity.TournamentId = "00" + pickEntity.TournamentId;
            }
            if (!data.ContainsKey(userId))
                data[userId] = new List<PickRecord>();
            var pickRecord = new PickRecord
            {
                Season = season,
                Tour = pickEntity.Tour,
                TournamentIndex = pickEntity.TournamentIndex,
                TournamentName = (string)tournament["Name"],
                TournamentId = pickEntity.TournamentId,
                UserId = userId,
                PlayerId = playerId,
                PlayerName = playerName,
            };
            data[userId].Add(pickRecord);

            var profile = profiles.SingleOrDefault(x => (string)x["UserId"] == userId);

            var key = $"{(string)profile["LastFirst"]}:{userId}";
            if (summary[key] == null)
            {
                summary[key] = new JArray();
            }
            var profileSummary = summary[key] as JArray;
            var count = 1;
            foreach(JObject pastPick in profileSummary)
            {
                if ((string)pastPick["PlayerId"] == pickRecord.PlayerId)
                    count++;
            }
            profileSummary.Add(JObject.FromObject(new
            {
                Season = season,
                Tour = pickEntity.Tour,
                TournamentIndex = pickEntity.TournamentIndex,
                TournamentName = (string)tournament["Name"],
                TournamentId = pickEntity.TournamentId,
                UserId = userId,
                UserName = (string)profile["Name"],
                LastFirst = (string)profile["LastFirst"],
                PlayerId = playerId,
                PlayerName = playerName,
                Instance = count,
            }));
        }
    }
    foreach (var item in data)
    {
        var path = $"{season}/PGA TOUR/{item.Key}.json";
        var value = JArray.FromObject(item.Value).ToString(Formatting.Indented);
        await blobService.UploadBlobAsync("pickhistory", path, value);
    }

    Sort(summary);
    await blobService.UploadBlobAsync("pickhistory", $"{season}/PGA TOUR/summary.json", summary.ToString(Formatting.Indented));

    var end = DateTime.UtcNow;
    log.Info($"Execution Time: {end - start}");
}

public static void Sort(JObject jObj)
{
    var props = jObj.Properties().ToList();
    foreach (var prop in props)
    {
        prop.Remove();
    }

    foreach (var prop in props.OrderBy(p => p.Name))
    {
        jObj.Add(prop);
        if (prop.Value is JObject)
            Sort((JObject)prop.Value);
    }
}

public class PickRecord
{
    public int Season { get; set; }
    public string Tour { get; set; }
    public string TournamentIndex { get; set; }
    public string TournamentId { get; set; }
    public string TournamentName { get; set; }
    public string UserId { get; set; }
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
}