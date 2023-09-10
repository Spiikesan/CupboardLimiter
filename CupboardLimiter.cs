using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using UnityEngine;

#region Changelogs and ToDo
/**********************************************************************
 * 
 * v1.1.0   :   Started to Maintain (Krungh Crow)
 *          :   Fixed RPC error
 *          :   Added permissions
 *          :   Language File Updated
 *          :   Debug warning added (read .cs file)
 *          :   Rewrite of config handling
 *          :   Coding Optimisations
 * v1.2.0   :   Added discord notification for exceeding limit
 *          :   Added Remaining tc chat notification after 1st placement
 * V1.2.1   :   Fix for remaining tc limit chatmessage
 * v1.2.2   :   Performance Update
 * v1.2.3   :   Possible fix for NRE at startup
 * v1.3.0   :   Maintain (Spiikesan): Rework done. Performance increased and bugs fixed.
 * v1.3.1   :   Lang messages are now version dependant (CANCELLED)
 * v1.4.0   :   Add command 'clinspect' to retrieve users TCs informations
 * v1.5.0   :   "Limit reached" message is now working.
 *          :   Chat prefix and icon is customizable.
 *          :   Dynamic limits and permissions.
 * v1.6.0   :   Add team-based limits.
 * v1.6.1   :   Refactoring team-based limits
 * V1.6.2   :   Command "tc" now use the player instead of the argument if there is no argument.
 **********************************************************************/
#endregion

namespace Oxide.Plugins
{
    [Info("Cupboard Limiter", "Spiikesan", "1.7.6")]
    [Description("Simplified version for cupboard limits")]

    public class CupboardLimiter : RustPlugin
    {
        #region Variables

        bool debug = false;

        const string Vip_Perm = "cupboardlimiter.vip";
        const string Bypass_Perm = "cupboardlimiter.bypass";
        const string Admin_Perm = "cupboardlimiter.admin";
        const string Other_Perm = "cupboardlimiter.limit_";
        const string CommandList_Perm = "cupboardlimiter.commandList";

        const string Message_MaxLimitDefault = "MaxLimitDefault";
        const string Message_MaxLimitVip = "MaxLimitVip";
        const string Message_MaxLimit = "MaxLimit";
        const string Message_Remaining = "Remaining";
        const string Message_NoPermission = "NoPermission";
        const string Message_Inspect = "cInspect";
        const string Message_InspectOwn = "cInspectOwn";
        const string Message_InspectNotFound = "cInspectNotFound";
        const string Message_InspectUsage = "cInspectUsage";
        const string Message_TeamOvercount = "TeamOvercount";
        const string Message_TeamOvercountTarget = "TeamOvercountTarget";
        const string Message_Error = "Error";


        Dictionary<ulong, List<BuildingPrivlidge>> TCIDs = new Dictionary<ulong, List<BuildingPrivlidge>>();
        private int TCCount(BasePlayer player)
        {
            //bypass command : ignore TC from player with bypass ?
            List<BuildingPrivlidge> tcs;
            int count = 0;

            if (configData.Limits.GlobalTeamLimit && player.Team != null && player.Team.members.Count > 1)
            {
                foreach (var pl in player.Team.members)
                {
                    if (TCIDs.TryGetValue(pl, out tcs))
                    {
                        count += tcs.Count;
                    }
                }
                // Counting pending invites to avoid bypass
                foreach (var pl in player.Team.invites.Except(player.Team.members))
                {
                    if (TCIDs.TryGetValue(pl, out tcs))
                    {
                        count += tcs.Count;
                    }
                }
            }
            else
            {
                if (TCIDs.TryGetValue(player.userID, out tcs))
                {
                    count = tcs.Count;
                }
            }
            return count;
        }

        private void TCAdd(ulong playerId, BuildingPrivlidge tcId)
        {
            List<BuildingPrivlidge> tcs;
            if (TCIDs.TryGetValue(playerId, out tcs))
            {
                if (!tcs.Contains(tcId))
                    tcs.Add(tcId);
            }
            else
            {
                tcs = new List<BuildingPrivlidge>();
                tcs.Add(tcId);
                TCIDs.Add(playerId, tcs);
            }
        }

        private void TCDel(ulong playerId, BuildingPrivlidge tcId)
        {
            List<BuildingPrivlidge> tcs;
            if (TCIDs.TryGetValue(playerId, out tcs))
            {
                tcs.Remove(tcId);
            }
        }

        #endregion

        #region Configuration

        void Init()
        {
            if (!LoadConfigVariables())
            {
                Puts("Config file issue detected. Please delete file, or check syntax and fix.");
                return;
            }
            permission.RegisterPermission(Vip_Perm, this);
            permission.RegisterPermission(Bypass_Perm, this);
            permission.RegisterPermission(Admin_Perm, this);
            permission.RegisterPermission(CommandList_Perm, this);

            for (int i = 0; i < configData.Limits.OtherLimits.Count; i++)
            {
                permission.RegisterPermission(Other_Perm + (i + 1), this);
            }

            if (debug) Puts($"Debug is activated check CupboardLimiter.cs file if not intended");
        }

        private ConfigData configData;

        class ConfigData
        {
            [JsonProperty(PropertyName = "Max amount of TC(s) to place")]
            public Settings Limits = new Settings();
            [JsonProperty(PropertyName = "Discord Notification")]
            public SettingsDiscord Discord = new SettingsDiscord();
            [JsonProperty(PropertyName = "Chat Settings")]
            public SettingsChat Chat = new SettingsChat();
        }

        class Settings
        {
            [JsonProperty(PropertyName = "Limit Default")]
            public int DefaultLimit = 1;
            [JsonProperty(PropertyName = "Limit Vip")]
            public int VipLimit = 3;
            [JsonProperty(PropertyName = "Limit Others")]
            public List<int> OtherLimits = new List<int>();
            [JsonProperty(PropertyName = "Limit Others Can Downgrade Default")]
            public bool OtherLimitsOverDefault = false;
            [JsonProperty(PropertyName = "Global Team Limit")]
            public bool GlobalTeamLimit = true;
            [JsonProperty(PropertyName = "Limits In Team")]
            public Dictionary<int, int> TeamLimits = new Dictionary<int, int>();
        }

        class SettingsDiscord
        {
            [JsonProperty(PropertyName = "Discord Webhook URL")]
            public string DiscordWebhookAddress = string.Empty;
        }

        class SettingsChat
        {
            [JsonProperty(PropertyName = "Prefix")]
            public string Prefix = "[Cupboard Limiter] :";
            [JsonProperty(PropertyName = "Icon's SteamId")]
            public ulong SteamIdIcon = 76561198049668039;

        }

        private bool LoadConfigVariables()
        {
            try
            {
                configData = Config.ReadObject<ConfigData>();
                foreach (var data in configData.Limits.TeamLimits)
                {
                    if (data.Key <= 1)
                    {
                        Puts($"Config Warning : Team count {data.Key} is below the limit of 2 players.");
                    }
                    else if (data.Key > RelationshipManager.maxTeamSize)
                    {
                        Puts($"Config Warning : Team count {data.Key} is over the max team limit of {RelationshipManager.maxTeamSize} players.");
                    }
                }
            }
            catch (Exception ex)
            {
                PrintError("An error occured dugin ConfigData load: " + ex.ToString());
                return false;
            }
            SaveConf();
            return true;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Fresh install detected Creating a new config file.");
            configData = new ConfigData();
            SaveConf();
        }

        void SaveConf() => Config.WriteObject(configData, true);
        #endregion

        #region LanguageAPI
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Message_MaxLimitDefault] = "You have reached the Default maximum cupboard limit of {0}",
                [Message_MaxLimitVip] = "You have reached the Vip maximum cupboard limit of {0}",
                [Message_MaxLimit] = "You have reached the maximum cupboard limit of {0}",
                [Message_Remaining] = "Amount of TC's remaining = {0}",
                [Message_NoPermission] = "You don't have the permission.",
                [Message_Inspect] = "The user {0} have {1} TCs.",
                [Message_InspectOwn] = "You have {0} TC and {1} remaining.",
                [Message_InspectNotFound] = "Error: User not found",
                [Message_TeamOvercount] = "You cannot invite this player right now, he have {0} TC too many.",
                [Message_TeamOvercountTarget] = "You cannot be invited by this player right now, you have {0} TC too many.",
                [Message_InspectUsage] = "Usage: tc <partialNameOrId> ...",
                [Message_Error] = "An unexpected error occured.",
            }, this, "en");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Message_MaxLimitDefault] = "Vous avez atteint la limite par defaut de {0} pour les armoires a outils.",
                [Message_MaxLimitVip] = "Vous avez atteint la limite VIP de {0} pour les armoires a outils.",
                [Message_MaxLimit] = "Vous avez atteint la limite de {0} pour les armoires a outils.",
                [Message_Remaining] = "Il vous reste {0} armoires a outils a placer.",
                [Message_NoPermission] = "Vous n'avez pas la permission",
                [Message_Inspect] = "Le joueur {0} a {1} TCs.",
                [Message_InspectOwn] = "Vous avez {0} TC(s) et {1} restant a poser.",
                [Message_InspectNotFound] = "Erreur: Le joueur n'a pas ete trouve.",
                [Message_TeamOvercount] = "Vous ne pouvez pas inviter ce joueur actuellement, il a {0} armoires a outils en trop.",
                [Message_TeamOvercountTarget] = "Vous ne pouvez pas etre invite par ce joueur actuellement, vous avez {0} armoires a outils en trop.",
                [Message_InspectUsage] = "Usage: tc <pseudoPartielOuId> ...",
                [Message_Error] = "Une erreur inattendue s'est produite.",
            }, this, "fr");
        }

        #endregion

        #region Hooks

        void OnServerInitialized()
        {
            if (!configData.Discord.DiscordWebhookAddress.Contains("discord.com/api/webhooks"))
            {
                Puts("Warning !!\n----------------------------------\nNo Webhook has been assigned yet !\n----------------------------------");
            }
            else if (configData.Discord.DiscordWebhookAddress.Contains("discord.com/api/webhooks"))
            {
                Puts($"Verified !!\n----------------------------------\nWebhook has been found :\n{configData.Discord.DiscordWebhookAddress}\n----------------------------------");
            }

            foreach (var entity in BaseNetworkable.serverEntities)
            {
                BuildingPrivlidge TC = entity as BuildingPrivlidge;
                if (TC != null && !TC.IsDestroyed && TC.OwnerID.IsSteamId())
                {
                    TCAdd(TC.OwnerID, TC);
                }
            }

            if (debug)
            {
                foreach (var userTc in TCIDs)
                {
                    try
                    {
                        Puts("User " + BasePlayer.allPlayerList.Single(pl => pl.userID == userTc.Key) + " has " + userTc.Value.Count + " TC : ");
                    }
                    catch
                    {
                        Puts("User " + userTc.Key + " has " + userTc.Value.Count + " TC : ");
                    }
                    foreach (var tc in userTc.Value)
                    {
                        Puts("   - " + tc);
                    }
                }
            }
        }

        void OnEntitySpawned(BaseEntity entity, UnityEngine.GameObject gameObject)
        {
            BuildingPrivlidge TC = entity as BuildingPrivlidge;

            if (TC != null && TC.OwnerID.IsSteamId())
            {
                TCAdd(TC.OwnerID, TC);

                if (debug) Puts($"a cupboard is spawning in world. It's ID is {TC.GetInstanceID()} and owner {TC.OwnerID}");

                BasePlayer player = BasePlayer.FindByID(TC.OwnerID);
                if (player == null) return;
                if (player.IsSleeping() || !player.IsConnected)
                {
                    if (debug) Puts($"sleep|offline check");
                    return;
                }

                // HOW MANY CHECK

                if (!permission.UserHasPermission(player.UserIDString, Bypass_Perm))
                {
                    int limit = GetTCLimit(player);
                    int count = TCCount(player);

                    if (debug) Puts($"{player}: cupboard count {count}");
                    // EXTRA CHECK IF PLAYER HAS ABNORMAL CUPBOARD COUNT
                    if (count - limit > 1) PrintWarning($"PLAYER {player.displayName} has {count - limit - 1} more cupboards over his limit of {limit} !");
                    // CANCEL IF LIMIT REACHED

                    if (count > limit)
                    {
                        NextTick(() =>
                        {
                            if (configData.Discord.DiscordWebhookAddress.Contains("discord.com/api/webhooks"))
                            {
                                try
                                {
                                    webrequest.Enqueue(configData.Discord.DiscordWebhookAddress, $"{{\"content\":\" {DateTime.Now.ToShortTimeString()} : **{player}** : Tried to place more TC's than the max of **{limit}**\"}}",
                                    Callback, this, RequestMethod.POST, new Dictionary<string, string> { ["Content-Type"] = "application/json" });
                                }
                                catch { }
                            }
                            RefundTC(TC, player);
                        });
                    }
                    else
                        ChatMessage(player, FormatMessage(Message_Remaining, player.UserIDString, limit - count));
                }
            }
            return;
        }

        void OnEntityKill(BaseEntity entity)
        {
            BuildingPrivlidge TC = entity as BuildingPrivlidge;

            if (TC is BuildingPrivlidge && TC.OwnerID.IsSteamId())
            {
                TCDel(TC.OwnerID, TC);
            }
        }

        object OnTeamInvite(BasePlayer inviter, BasePlayer target)
        {
            if (configData.Limits.GlobalTeamLimit)
            {
                int limit = GetTCLimit(inviter, true);
                int teamTC = TCCount(inviter);
                int targetTC = TCCount(target);

                if (teamTC + targetTC > limit)
                {
                    ChatMessage(inviter, FormatMessage(Message_TeamOvercount, inviter.UserIDString, (teamTC + targetTC) - limit));
                    ChatMessage(target, FormatMessage(Message_TeamOvercountTarget, target.UserIDString, (teamTC + targetTC) - limit));
                    inviter.Team.RejectInvite(target);
                    return false;
                }
            }
            return null;
        }

        #endregion

        #region Commands

        [ChatCommand("tc")]
        private void ChatCommand_Inspect(BasePlayer player, string command, string[] args)
        {

            ulong userID = player.userID;
            bool isOwn = true;
            string receiverId = player.UserIDString;
            if (args.Length >= 1)
            {
                isOwn = false;
                userID = 0;
                if (permission.UserHasPermission(player.UserIDString, CommandList_Perm))
                {
                    var user = covalence.Players.FindPlayer(args[0]);

                    if (user is IPlayer)
                    {
                        if (ulong.TryParse(user.Id, out userID))
                        {
                            player = user.Object as BasePlayer;
                        }
                    }
                    else
                    {
                        ChatMessage(player, FormatMessage(Message_InspectNotFound, player.UserIDString));
                    }
                }
                else
                {
                    ChatMessage(player, FormatMessage(Message_NoPermission, player.UserIDString));
                }
            }

            if (player != null && userID.IsSteamId())
            {
                ChatMessage(player, PlayerTcsString(player, receiverId, isOwn));
            }
            else
            {
                ChatMessage(player, FormatMessage(Message_Error, player.UserIDString));
            }
        }
        [ConsoleCommand("tc")]
        private void ServerCommand_Inspect(ConsoleSystem.Arg arg)
        {
            PrintToConsole("ServerCommand");
            if (!arg.IsServerside)
            {
                arg.ReplyWith("Unkown command.");
            }
            else if (arg.Args == null || arg.Args.Count() == 0)
            {
                arg.ReplyWith(FormatMessage(Message_InspectUsage, null));
            }
            else
            {
                string reply = "Query result:\n";
                foreach (string playerId in arg.Args)
                {
                    var user = covalence.Players.FindPlayer(playerId);
                    if (user != null)
                    {
                        BasePlayer player = user.Object as BasePlayer;
                        if (player != null)
                        {
                            reply += $"{PlayerTcsString(player, null, false)}\n";
                        }
                        else
                        {
                            reply += $"Player \"{playerId}\" cannot be got as BasePlayer\n";
                        }
                    }
                    else
                    {
                        reply += $"Player \"{playerId}\" not found\n";
                    }
                }
                arg.ReplyWith(reply);
            }
        }


        #endregion

        #region Helpers

        void RefundTC(BaseEntity tC, BasePlayer player)
        {
            if (debug) Puts($"cancelling cupboard ID {tC.GetInstanceID()} of player {player.UserIDString}");

            if (permission.UserHasPermission(player.UserIDString, Vip_Perm))
            {
                ChatMessage(player, FormatMessage(Message_MaxLimitVip, player.UserIDString, configData.Limits.VipLimit));
            }
            else
            {
                bool otherLimitPerm = false;
                for (int i = 0; i < configData.Limits.OtherLimits.Count; i++)
                {
                    if (permission.UserHasPermission(player.UserIDString, Other_Perm + (i + 1)))
                    {
                        otherLimitPerm = true;
                        break;
                    }
                }
                if (otherLimitPerm)
                {
                    ChatMessage(player, FormatMessage(Message_MaxLimit, player.UserIDString, configData.Limits.DefaultLimit));
                }
                else
                {
                    ChatMessage(player, FormatMessage(Message_MaxLimitDefault, player.UserIDString, configData.Limits.DefaultLimit));
                }
            }

            tC.KillMessage();
            var itemToGive = ItemManager.CreateByItemID(-97956382, 1);
            if (itemToGive != null) player.inventory.GiveItem(itemToGive);
        }

        public int GetTCLimit(BasePlayer player, bool isInvite = false)
        {
            int limit = configData.Limits.DefaultLimit;

            if (debug) Puts($"{player}: Default limit {limit}");

            if (configData.Limits.TeamLimits.Count > 0 && player.Team != null)
            {
                int tcount = player.Team.members.Count + (isInvite ? 1 : 0);

                if (tcount > 1)
                {
                    foreach (var tlim in configData.Limits.TeamLimits)
                    {
                        if (tlim.Key <= tcount)
                            limit = tlim.Value;
                        else
                            break;
                    }
                    if (debug) Puts($"{player}: Team limit {limit} for {tcount} players in the team");
                }
            }

            if (configData.Limits.OtherLimits.Count > 0)
            {
                int olimit = -1;
                for (int i = 0; i < configData.Limits.OtherLimits.Count; i++)
                {
                    if (permission.UserHasPermission(player.UserIDString, Other_Perm + (i + 1)))
                    {
                        if (configData.Limits.OtherLimits[i] > olimit)
                        {
                            olimit = configData.Limits.OtherLimits[i];
                        }
                    }
                }
                if (olimit > 0 && (olimit > limit || configData.Limits.OtherLimitsOverDefault))
                {
                    limit = olimit;
                }
                if (debug) Puts($"{player}: Other limit {olimit}");
            }

            if (permission.UserHasPermission(player.UserIDString, Vip_Perm))
            {
                limit = configData.Limits.VipLimit;
                if (debug) Puts($"{player}: VIP limit {limit}");
            }

            return limit;
        }

        public void Callback(int code, string response)
        {
        }

        public string FormatMessage(string messageId, string userId, params object[] parameters)
        {
            return string.Format(lang.GetMessage(messageId, this, userId), parameters);
        }

        private void ChatMessage(BasePlayer player, string message)
        {
            Player.Message(player, message, configData.Chat.Prefix, configData.Chat.SteamIdIcon);
        }

        private string PlayerTcsString(BasePlayer player, string receiverId, bool isOwn)
        {
            List<BuildingPrivlidge> tcs;
            if (!TCIDs.TryGetValue(player.userID, out tcs))
                tcs = new List<BuildingPrivlidge>();
            tcs = tcs.FindAll(tc => !tc.IsDestroyed);
            string msg = isOwn ? FormatMessage(Message_InspectOwn, receiverId, tcs.Count, GetTCLimit(player) - TCCount(player))
                               : FormatMessage(Message_Inspect, receiverId, player.displayName, tcs.Count);
            foreach (var TC in tcs)
            {
                msg += "\n - Pos: " + GetCoordinates(TC.ServerPosition);
            }
            return msg;
        }

        private string GetCoordinates(Vector3 position)
        {
            const float CELL_SIZE = 150f;
            Vector3 realPos = position - TerrainMeta.Transform.position; //Getting the real position in the "player map"
            realPos.z = TerrainMeta.Size.z - realPos.z; //Top is 0, we need to invert Z axis

            Vector3 error = new Vector3(-90f, 0f, -90f);
            Vector3 correction = new Vector3(
                error.x * (realPos.x / (TerrainMeta.Size.x + error.x)),
                0,
                error.z * (realPos.z / TerrainMeta.Size.z) - error.z
                );

            Vector3 cellPos = (realPos - correction) / CELL_SIZE;


            int letter = (int)cellPos.x;
            string c = string.Empty;
            if (letter >= 26)
            {
                c += (char)((letter / 26 - 1) + 'A');
                letter %= 26;
            }
            c += (char)(letter + 'A');

            c += ((int)cellPos.z).ToString();
            return c;
        }

        #endregion
    }
}