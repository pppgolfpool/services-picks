#r "..\Common\PppPool.Common.dll"
#r "..\Common\Microsoft.WindowsAzure.Storage.dll"
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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

public class PickEntity : TableEntity
{
    public PickEntity()
    {

    }

    public PickEntity(int season, string tourName, string userId, string email, string playerId, string playerName, string tournamentId, string tournamentIndex)
    {
        PartitionKey = $"{season}:{tourName}:{tournamentIndex}";
        RowKey = $"{userId}";
        UserId = userId;
        UserEmail = email;
        PlayerId = playerId;
        PlayerName = playerName;
        Season = season;
        Tour = tourName;
        TournamentIndex = tournamentIndex;
        TournamentId = tournamentId;
    }

    public string UserId { get; set; }

    public string UserEmail { get; set; }

    public string PlayerId { get; set; }

    public string PlayerName { get; set; }

    public int Season { get; set; }

    public string Tour { get; set; }

    public string TournamentIndex { get; set; }

    public string TournamentId { get; set; }
}