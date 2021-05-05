using EXILED;
using EXILED.Extensions;
using EXILED.Patches;
using MEC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Utf8Json;

namespace IBC_Store
{
    public class MainClass : Plugin
    {
        public override string getName
        {
            get
            {
                return "IBC-Store";
            }
        }

        public class TempRank
        {
            public string UserID { get; set; } = "";
            public string Rank { get; set; } = "";
            public string DateTime { get; set; } = "";
        }

        public override void OnDisable()
        {
        }

        public static string appData;
        public List<TempRank> tempRanks;

        public override void OnEnable()
        {
            JsonSerializer.Serialize(new List<TempRank>());
            appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            if (!Directory.Exists(Path.Combine(appData, "Plugins", "IBC Store")))
                Directory.CreateDirectory(Path.Combine(appData, "Plugins", "IBC Store"));
            if (!File.Exists(Path.Combine(appData, "Plugins", "IBC Store", "rangi.json")))
                File.WriteAllText(Path.Combine(appData, "Plugins", "IBC Store", "rangi.json"), Encoding.UTF8.GetString(JsonSerializer.Serialize(new List<TempRank>())));
            tempRanks = JsonSerializer.Deserialize<List<TempRank>>(Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(appData, "Plugins", "IBC Store", "rangi.json"))));
            Events.RemoteAdminCommandEvent += Events_RemoteAdminCommandEvent;
            Events.PlayerJoinEvent += Events_PlayerJoinEvent;
            Events.WaitingForPlayersEvent += Events_WaitingForPlayersEvent;
            Timing.RunCoroutine(CheckRankExpiration());
        }

        public DateTime GetDateTimeWarsaw()
        {
            TimeZoneInfo destinationTimeZone2 = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
            DateTime dateTime = TimeZoneInfo.ConvertTime(DateTime.Now, destinationTimeZone2);
            return dateTime;
        }

        public IEnumerator<float> CheckRankExpiration()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(10f);
                tempRanks = JsonSerializer.Deserialize<List<TempRank>>(Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(appData, "Plugins", "IBC Store", "rangi.json"))));
                bool overwrite = false;
                foreach (TempRank tr in tempRanks.ToList())
                {
                    DateTime time = DateTime.Parse(tr.DateTime);
                    if (time < GetDateTimeWarsaw())
                    {
                        foreach (ReferenceHub hub in Player.GetHubs())
                        {
                            if (hub.GetUserId() == tr.UserID)
                            {
                                hub.serverRoles.SetGroup(null, false, false, false);
                            }
                        }
                        overwrite = true;
                        ServerConsole.AddLog("Usunieto range osobie " + tr.UserID);
                        tempRanks.Remove(tr);
                    }
                }
                if (overwrite)
                    File.WriteAllText(Path.Combine(appData, "Plugins", "IBC Store", "rangi.json"), Encoding.UTF8.GetString(JsonSerializer.Serialize(tempRanks)));
            }
        }

        private void Events_WaitingForPlayersEvent()
        {
            tempRanks = JsonSerializer.Deserialize<List<TempRank>>(Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(appData, "Plugins", "IBC Store", "rangi.json"))));
        }

        public UserGroup GetTempRank(string userID)
        {
            foreach (TempRank tr in tempRanks)
            {
                if (tr.UserID == userID)
                {
                    UserGroup gr = ServerStatic.GetPermissionsHandler().GetGroup(tr.Rank);
                    if (gr == null)
                    {
                        ServerConsole.AddLog(" Ranga o nazwie " + tr.Rank + " nie istnieje.");
                    }
                    else
                    {
                        return gr;
                    }
                }
            }
            return null;
        }

        public void AddTempRank(string userID, string rank, DateTime date)
        {
            tempRanks = JsonSerializer.Deserialize<List<TempRank>>(Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(appData, "Plugins", "IBC Store", "rangi.json"))));
            bool contains = false;
            foreach (TempRank tr in tempRanks)
            {
                if (tr.UserID == userID)
                {
                    tr.Rank = rank;
                    tr.DateTime = date.ToString();
                    contains = true;
                }
            }
            if (!contains)
                tempRanks.Add(new TempRank()
                {
                    UserID = userID,
                    Rank = rank,
                    DateTime = date.ToString()
                });
            File.WriteAllText(Path.Combine(appData, "Plugins", "IBC Store", "rangi.json"), Encoding.UTF8.GetString(JsonSerializer.Serialize(tempRanks)));
            ServerConsole.AddLog(" Nadano range " + rank + " osobie " + userID + " do " + date.ToString("HH:mm | dd.MM.yyyy"));
            foreach (ReferenceHub hub in Player.GetHubs())
            {
                if (hub.GetUserId() == userID)
                {
                    UserGroup gr = ServerStatic.GetPermissionsHandler().GetGroup(rank);
                    if (gr == null)
                    {
                        ServerConsole.AddLog(" Ranga o nazwie " + rank + " nie istnieje.");
                    }
                    else
                    {
                        hub.serverRoles.SetGroup(gr, false, true, true);
                    }
                }
            }
        }

        private void Events_PlayerJoinEvent(EXILED.PlayerJoinEvent ev)
        {
            UserGroup tempRank = GetTempRank(ev.Player.GetUserId());
            if (tempRank != null)
            {
                ev.Player.serverRoles.SetGroup(tempRank, false, true, true);
            }
        }

        private void Events_RemoteAdminCommandEvent(ref RACommandEvent ev)
        {
            string[] arr = ev.Command.Split(' ');
            switch(arr[0].ToLower())
            {
                case "temprank":
                    if (arr[1] == null || arr[2] == null || arr[3] == null)
                        break;
                    DateTime time = GetDateTimeWarsaw();
                    time = time.AddDays(double.Parse(arr[3]));
                    AddTempRank(arr[1], arr[2], time);
                    ev.Allow = false;
                    break;
                case "testws":
                    string command = ev.Command.Remove(0, 7);
                    string[] str = command.Split(';');
                    WebSocketInput(str);
                    ev.Allow = false;
                    break;
            }
        }

        public void WebSocketInput(string[] str)
        {
            foreach(string command in str)
            {
                ServerConsole.EnterCommand(command, null);
            }
        }

        public override void OnReload()
        {
        }
    }
}
