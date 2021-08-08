using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Newtonsoft.Json;

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
 * 
 **********************************************************************/
#endregion

namespace Oxide.Plugins
{
    [Info("Cupboard Limiter", "Spiikesan", "1.3.1")]
    [Description("Simplified version for cupboard limits")]

    public class CupboardLimiter : RustPlugin
    {
        #region Variables

        bool debug = false;

        const string Default_Perm = "cupboardlimiter.default";
        const string Vip_Perm = "cupboardlimiter.vip";
        const string Bypass_Perm = "cupboardlimiter.bypass";

        string Message_MaxLimitDefault = "MaxLimitDefault";
        string Message_MaxLimitVip = "MaxLimitVip";
        string Message_Remaining = "Remaining";

        Dictionary<ulong, List<int>> TCIDs = new Dictionary<ulong, List<int>>();
        private int TCCount(BasePlayer player)
        {
            List<int> tcs;

            if (TCIDs.TryGetValue(player.userID, out tcs))
            {
                return tcs.Count();
            }
            return 0;
        }

        private void TCAdd(ulong playerId, int tcId)
        {
            List<int> tcs;
            if (TCIDs.TryGetValue(playerId, out tcs))
            {
                if (!tcs.Contains(tcId))
                    tcs.Add(tcId);
            }
            else
            {
                tcs = new List<int>();
                tcs.Add(tcId);
                TCIDs.Add(playerId, tcs);
            }
        }

        private void TCDel(ulong playerId, int tcId)
        {
            List<int> tcs;
            if (TCIDs.TryGetValue(playerId, out tcs))
            {
                tcs.Remove(tcId);
            }
        }

        #endregion

        #region Configuration

        void Init()
        {
            Puts("########## Init");
            if (!LoadConfigVariables())
            {
                Puts("Config file issue detected. Please delete file, or check syntax and fix.");
                return;
            }
            permission.RegisterPermission(Default_Perm, this);
            permission.RegisterPermission(Vip_Perm, this);
            permission.RegisterPermission(Bypass_Perm, this);

            if (debug) Puts($"Debug is activated check CupboardLimiter.cs file if not intended");
        }

        private ConfigData configData;

        class ConfigData
        {
            [JsonProperty(PropertyName = "Max amount of TC(s) to place")]
            public Settings Limits = new Settings();
            [JsonProperty(PropertyName = "Discord Notification")]
            public SettingsDiscord Discord = new SettingsDiscord();
        }

        class Settings
        {
            [JsonProperty(PropertyName = "Limit Default")]
            public int DefaultLimit = 1;
            [JsonProperty(PropertyName = "Limit Vip")]
            public int VipLimit = 3;
        }

        class SettingsDiscord
        {
            [JsonProperty(PropertyName = "Discord Webhook URL")]
            public string DiscordWebhookAddress = "";
        }

        private bool LoadConfigVariables()
        {
            try
            {
                configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
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
            Message_MaxLimitDefault += "_" + Version;
            Message_MaxLimitVip += "_" + Version;
            Message_Remaining += "_" + Version;

            Puts("########## LoadDefaultMessages");
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Message_MaxLimitDefault] = "You have reached the Default maximum cupboard limit of {0}",
                [Message_MaxLimitVip] = "You have reached the Vip maximum cupboard limit of {0}",
                [Message_Remaining] = "Amount of TC's remaining = {0}",
            }, this, "en");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Message_MaxLimitDefault] = "Vous avez atteint la limite par défaut de {0} pour les armoires à outils.",
                [Message_MaxLimitVip] = "Vous avez atteint la limite VIP de {0} pour les armoires à outils.",
                [Message_Remaining] = "Il vous reste {0} armoires à outil à placer.",
            }, this, "fr");
        }

        #endregion

        #region Hooks

        void OnServerInitialized()
        {
            Puts("########## OnServerInitialized");
            if (!configData.Discord.DiscordWebhookAddress.Contains("discord.com/api/webhooks"))
            {
                Puts("Warning !!\n----------------------------------\nNo Webhook has been assigned yet !\n----------------------------------");
            }
            else if (configData.Discord.DiscordWebhookAddress.Contains("discord.com/api/webhooks"))
            {
                Puts($"Verified !!\n----------------------------------\nWebhook has been found :\n{configData.Discord.DiscordWebhookAddress}\n----------------------------------");
            }

            foreach (var TC in BaseNetworkable.serverEntities.OfType<BuildingPrivlidge>())
            {
                if (TC.OwnerID.IsSteamId())
                {
                    TCAdd(TC.OwnerID, TC.GetInstanceID());
                }
            }
            if (debug)
            {
                foreach (var userTc in TCIDs)
                {
                    try
                    {
                        Puts("User " + BasePlayer.allPlayerList.Single(pl => pl.userID == userTc.Key) + " has " + userTc.Value.Count + " TC : ");
                    } catch
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

        void OnEntitySpawned(BaseEntity TC, UnityEngine.GameObject gameObject)
        {
            if (TC is BuildingPrivlidge && TC.OwnerID.IsSteamId())
            {
                TCAdd(TC.OwnerID, TC.GetInstanceID());

                if (debug) Puts($"a cupboard is spawning in world. It's ID is {TC.GetInstanceID()} and owner {TC.OwnerID}");

                BasePlayer player = BasePlayer.FindByID(TC.OwnerID);
                if (player == null) return;
                if (player.IsSleeping() || !player.IsConnected) {
                    if (debug) Puts($"sleep|offline check");
                    return;
                }
                
                // HOW MANY CHECK

                if (!permission.UserHasPermission(player.UserIDString, Bypass_Perm))
                {
                    int limit = configData.Limits.DefaultLimit;
                    int count = TCCount(player);

                    if (debug) Puts($"{player}: Default limit {limit}");
                    if (permission.UserHasPermission(player.UserIDString, Vip_Perm))
                    {
                        limit = configData.Limits.VipLimit;
                        if (debug) Puts($"{player}: VIP limit {limit}");
                    }
                    else if (permission.UserHasPermission(player.UserIDString, Default_Perm))
                    {
                        limit = configData.Limits.DefaultLimit;
                        if (debug) Puts($"{player}: RE-Default limit {limit}");
                    }

                    if (debug) Puts($"{player}: cupboard count {count}");
                    // EXTRA CHECK IF PLAYER HAS ABNORMAL CUPBOARD COUNT
                    if (count - limit > 1) PrintWarning($"PLAYER {player.displayName} has {count - 1} more cupboards over his limit of {limit} !");
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
                            return;
                        });
                    }
                    else
                        SendReply(player, FormatMessage(Message_Remaining, player.UserIDString, (limit - count).ToString()));
                }
            }
            return;
        }

        void OnEntityKill(BaseEntity TC)
        {
            if (TC is BuildingPrivlidge && TC.OwnerID.IsSteamId())
            {
                TCDel(TC.OwnerID, TC.GetInstanceID());
            }
        }

        #endregion

        #region Helpers

        void RefundTC(BaseEntity TC, BasePlayer player)
        {
            if (debug) Puts($"cancelling cupboard ID {TC.GetInstanceID()} of player {player.UserIDString}");

            if (permission.UserHasPermission(player.UserIDString, Vip_Perm))
            {
                SendReply(player, FormatMessage(Message_MaxLimitVip, player.UserIDString, configData.Limits.VipLimit.ToString()));
            }
            else if (permission.UserHasPermission(player.UserIDString, Default_Perm))
            {
                SendReply(player, FormatMessage(Message_MaxLimitDefault, player.UserIDString, configData.Limits.DefaultLimit.ToString()));
            }

            TC.KillMessage();
            var itemToGive = ItemManager.CreateByItemID(-97956382, 1);
            if (itemToGive != null) player.inventory.GiveItem(itemToGive);
        }

        public void Callback(int code, string response)
        {
        }

        public string FormatMessage(string messageId, string userId, params string[] parameters)
        {
            return string.Format(lang.GetMessage(messageId, this, userId), parameters);
        }

        #endregion
    }
}