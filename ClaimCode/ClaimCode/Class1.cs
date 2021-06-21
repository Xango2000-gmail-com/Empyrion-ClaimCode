using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Eleon.Modding;
//using ProtoBuf;
using YamlDotNet.Serialization;
using Eleon;
using UnityEngine;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace ClaimCode
{
    public class MyEmpyrionMod : IMod,  ModInterface
    {
        internal static IModApi modApi;

        internal static string ModShortName = "ClaimCode";
        public static string ModVersion = "v1.0.0 made by Xango2000 (3140)";
        public static string ModPath = "..\\Content\\Mods\\ClaimCode\\";
        internal static bool debug = false;
        internal static Dictionary<int, Storage.StorableData> SeqNrStorage = new Dictionary<int, Storage.StorableData> { };
        public int thisSeqNr = 8000;
        internal static SetupYaml.Root SetupYamlData = new SetupYaml.Root { };
        internal static Dictionary<string, SetupYaml.Claimable> KeysDictionary = new Dictionary<string, SetupYaml.Claimable> { };
        internal static string Timestamp = "Timestamp";
        internal static List<string> NewEntityIDQueue = new List<string> { };
        internal static string VoteSiteURL = "";
        internal static ServerDetailsJSON ServerDetails;
        internal static VoteHistoryJSON VoteHistory;
        internal static Dictionary<string, int> VoteTracker = new Dictionary<string, int> { };
        
        internal static Dictionary<string, int> PlayfieldProcessIDDict = new Dictionary<string, int> { };

        internal static List<int> LoggingOn = new List<int> { };
        internal static string ModFolder = "";
        internal static string SaveGameName = "";



        //PfServer Only
        internal static IPlayfield Playfield;

        //Dedi Only
        internal static Dictionary<string, int> SteamToEmpyrionID = new Dictionary<string, int> { };
        internal static Dictionary<int, EntityData> EntityDataFromPfServer = new Dictionary<int, EntityData> { };

        public class EntityData
        {
            public string Playfield { get; set; }
            public string Name { get; set; }
        }

        //Expiration Crap
        internal static bool Disable = false;
        internal static int Expiration = 1628312399;
        internal static bool LiteVersion = false;
        internal static List<string> OnlinePlayers = new List<string> { };

        //########################################################################################################################################################
        //################################################ This is where the actual Empyrion Modding API1 stuff Begins ###########################################
        //########################################################################################################################################################
        public void Game_Start(ModGameAPI gameAPI)
        {
            Storage.GameAPI = gameAPI;
            if (File.Exists(ModPath + "ERROR.txt")) { File.Delete(ModPath + "ERROR.txt"); }
            if (File.Exists(ModPath + "debug.txt")) { File.Delete(ModPath + "debug.txt"); }
            Timestamp = CommonFunctions.TimeStampFilenameFriendly();
            //SetupYaml.Setup();
            Task junkTask = RestServerDetails();
            int[] TimestampArray = CommonFunctions.TimestampArray();
            if (File.Exists(ModPath + "Logs\\" + TimestampArray[0] + TimestampArray[1] + ".txt"))
            {
                string[] CountedVotes = File.ReadAllLines(ModPath + "Logs\\" + TimestampArray[0] + TimestampArray[1] + ".txt");
                foreach (string SteamID in CountedVotes)
                {
                    if (VoteTracker.Keys.Contains(SteamID))
                    {
                        VoteTracker[SteamID] = VoteTracker[SteamID] + 1;
                    }
                    else
                    {
                        VoteTracker.Add(SteamID, 1);
                    }
                }
            }

            if (File.Exists(ModPath + "RetrieveHistory.txt"))
            {
                Task VoteHistory = RestCurrentVoteCounts();
                File.Delete(ModPath + "RetrieveHistory.txt"); //***
            }

        }

        public void Game_Event(CmdId cmdId, ushort seqNr, object data)
        {
            try
            {
                switch (cmdId)
                {
                    case CmdId.Event_ChatMessage:
                        //Triggered when player says something in-game
                        ChatInfo Received_ChatInfo = (ChatInfo)data;
                        string ChatMsg_SteamID = modApi.Application.GetPlayerDataFor(Received_ChatInfo.playerId).Value.SteamId;
                        string msg = Received_ChatInfo.msg.ToLower();
                        string msg2 = "";
                        if (msg.Contains(' '))
                        {
                            try
                            {
                                string[] msgArray = msg.Split(' ');
                                msg2 = msgArray[1];
                            }
                            catch { }
                        }


                        if (msg.ToLower() == "/mods" || msg.ToLower() == "!mods")
                        {
                            //API.Chat("Player", Received_ChatInfo.playerId, ModVersion);
                            API.ServerTell(Received_ChatInfo.playerId, ModShortName, ModVersion, true);
                        }
                        else if (msg.ToLower() == "/debug claimcode")
                        {
                            if (debug)
                            {
                                debug = false;
                                //API.Chat("Player", Received_ChatInfo.playerId, "ClaimCode: Debug is now False");
                                API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Debug is now False", true);
                            }
                            else
                            {
                                debug = true;
                                //API.Chat("Player", Received_ChatInfo.playerId, "ClaimCode: Debug is now True");
                                API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Debug is now True", true);
                            }
                        }
                        else if (msg.ToLower() == SetupYamlData.General.ReinitializeCommand.ToLower())
                        {
                            SetupYaml.Setup();
                            //API.Chat("Player", Received_ChatInfo.playerId, "ClaimCode Reinitialized");
                            API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Reinitialized", true);
                        }
                        #region MovedtoAPI2
                        /*
                        else if (msg.ToLower().Contains(SetupYamlData.General.ClaimCommand.ToLower()))
                        {
                            //Open Popup Window, on close check code against KeysDictionary.keys
                            string Claimables = "Your current rewards: \r\n";
                            VoteRewardYaml.Root CurrentRewards = new VoteRewardYaml.Root { };
                            if (File.Exists(ModPath + "VoteRewards\\" + ChatMsg_SteamID + ".yaml"))
                            {
                                CurrentRewards = VoteRewardYaml.ReadYaml(ModPath + "VoteRewards\\" + ChatMsg_SteamID + ".yaml");
                                if (CurrentRewards.HealMe > 0)
                                {
                                    Claimables = Claimables + SetupYamlData.VoteReward.HealMeCommand + " x" + CurrentRewards.HealMe + "\r\n";
                                }
                                if (CurrentRewards.RaffleTickets > 0)
                                {
                                    Claimables = Claimables + "RaffleTickets x" + CurrentRewards.RaffleTickets + "\r\n";
                                }
                                if (CurrentRewards.Credits > 0)
                                {
                                    Claimables = Claimables + "Credits x" + CurrentRewards.Credits + "\r\n";
                                }
                                if (CurrentRewards.ItemStacks.Count > 0)
                                {
                                    Claimables = Claimables + "Items x?\r\n";
                                }
                                if (CurrentRewards.Claimables.Count() > 0)
                                {
                                    Claimables = Claimables + "\r\nItem Packs, Vessels and Bases" + "\r\n";
                                    foreach (SetupYaml.Claimable Claimable in CurrentRewards.Claimables)
                                    {
                                        Claimables = Claimables + Claimable.NickName + "\r\n";
                                    }
                                }
                            }
                            Claimables = Claimables + "\r\nFor Website Purchases: Type the code you received in the box below to claim your reward.";
                            DialogConfig newDialog = new DialogConfig
                            {
                                TitleText = "Claimables",
                                BodyText = Claimables,
                                ButtonIdxForEnter = 0,
                                ButtonIdxForEsc = 1,
                                ButtonTexts = new string[2] { "Claim", "Cancel" },
                                CloseOnLinkClick = false,
                                InitialContent = "",
                                MaxChars = 1000,
                                Placeholder = "Type Code Here to claim"
                            };
                            CommonFunctions.Debug("before set msg2");
                            if (msg.Contains(' ')) newDialog.InitialContent = msg2;
                            CommonFunctions.Debug("after set msg2");
                            DialogActionHandler DialogHandler = new DialogActionHandler(OnDialogBoxClosed);
                            modApi.Application.ShowDialogBox(Received_ChatInfo.playerId, newDialog, DialogHandler, 10);
                        }
                        else if (msg.ToLower().Contains(SetupYamlData.General.InsuranceCommand.ToLower()))
                        {
                            //Check to see if the vessel is insured FIRST
                            //string SteamID = DB.SteamID(Received_ChatInfo.playerId);
                            PlayerData? PlayerData = modApi.Application.GetPlayerDataFor(Received_ChatInfo.playerId);
                            //string SteamID = PlayerData.Value.SteamId;

                            string BodyText = "Warning: Vessel Inventory will be deleted.\r\n Which of these vessels do you wish to file a claim on?\r\n";
                            try
                            {
                                Insurance.Root InsuranceYaml = Insurance.ReadYaml(ModPath + "Insurance\\" + ChatMsg_SteamID + ".yaml");
                                foreach (Insurance.InsuredEntity Entity in InsuranceYaml.InsuredEntities)
                                {
                                    try
                                    {
                                        DB.EntityData EntityData = DB.LookupEntity(Entity.EntityID);
                                        if (Entity.InsuranceRemaining > 0)
                                        {
                                            BodyText = BodyText + Entity.EntityID + "  " + EntityData.Name + "\r\n";
                                        }
                                    }
                                    catch { }
                                }
                                DialogConfig newDialog = new DialogConfig
                                {
                                    TitleText = "Claim Insurance",
                                    BodyText = BodyText,
                                    ButtonIdxForEnter = 0,
                                    ButtonIdxForEsc = 1,
                                    ButtonTexts = new string[2] { "File", "Cancel" },
                                    CloseOnLinkClick = false,
                                    InitialContent = "",
                                    MaxChars = 20,
                                    Placeholder = "Type Vessel ID"
                                };
                                if (msg.Contains(' ')) newDialog.InitialContent = msg2;
                                DialogActionHandler DialogHandler = new DialogActionHandler(ClaimInsurance);
                                modApi.Application.ShowDialogBox(Received_ChatInfo.playerId, newDialog, DialogHandler, 10);
                            }
                            catch
                            {
                                API.ServerTell(Received_ChatInfo.playerId, ModShortName, "You Don\'t have any insurance policies.", true);
                            }

                        }
                        else if (msg.ToLower().Contains(SetupYamlData.VoteReward.Command.ToLower()))
                        {
                            Task returned = RestCheckVote(Received_ChatInfo.playerId);
                        }
                        else if (msg.ToLower().Contains(SetupYamlData.VoteReward.HealMeCommand.ToLower()))
                        {
                            //Request PlayerInfo
                            Storage.StorableData StorableData = new Storage.StorableData
                            {
                                function = "HealMe",
                                Match = Convert.ToString(Received_ChatInfo.playerId),
                                Requested = "PlayerInfo",
                                ChatInfo = Received_ChatInfo
                            };
                            API.PlayerInfo(Received_ChatInfo.playerId, StorableData);
                        }
                        */
                        #endregion
                        break;


                    case CmdId.Event_Player_Connected:
                        //Triggered when a player logs on
                        Id Received_PlayerConnected = (Id)data;
                        if ( modApi.Application.Mode == ApplicationMode.DedicatedServer)
                        {
                            if (!LoggingOn.Contains(Received_PlayerConnected.id))
                            {
                                LoggingOn.Add(Received_PlayerConnected.id);
                                try
                                {
                                    Task<string> HasVoted = RestCheckVote(Received_PlayerConnected.id);
                                }
                                catch (Exception ex)
                                {
                                    CommonFunctions.ERROR("Message: " + ex.Message);
                                    CommonFunctions.ERROR("Data: " + ex.Data);
                                    CommonFunctions.ERROR("HelpLink: " + ex.HelpLink);
                                    CommonFunctions.ERROR("InnerException: " + ex.InnerException);
                                    CommonFunctions.ERROR("Source: " + ex.Source);
                                    CommonFunctions.ERROR("StackTrace: " + ex.StackTrace);
                                    CommonFunctions.ERROR("TargetSite: " + ex.TargetSite);
                                }
                                CommonFunctions.Debug(Received_PlayerConnected.id + " end");
                            }

                            string ConnectingPlayerSteamID = modApi.Application.GetPlayerDataFor(Received_PlayerConnected.id).Value.SteamId;
                            if (!OnlinePlayers.Contains(ConnectingPlayerSteamID))
                            {
                                OnlinePlayers.Add(ConnectingPlayerSteamID);
                            }
                            if (OnlinePlayers.Count > 10 && LiteVersion)
                            {
                                Disable = true;
                            }
                            else if (Expiration < int.Parse(CommonFunctions.UnixTimeStamp()))
                            {
                                Disable = true;
                            }
                            SteamToEmpyrionID[ConnectingPlayerSteamID] = Received_PlayerConnected.id;
                        }
                        break;


                    case CmdId.Event_Player_Disconnected:
                        //Triggered when a player logs off
                        Id Received_PlayerDisconnected = (Id)data;
                        break;


                    case CmdId.Event_Player_ChangedPlayfield:
                        //Triggered when a player changes playfield
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_ChangePlayfield, (ushort)CurrentSeqNr, new IdPlayfieldPositionRotation( [PlayerID], [Playfield Name], [PVector3 position], [PVector3 Rotation] ));
                        IdPlayfield Received_PlayerChangedPlayfield = (IdPlayfield)data;
                        break;


                    case CmdId.Event_Playfield_Loaded:
                        //Triggered when a player goes to a playfield that isnt currently loaded in memory
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Load_Playfield, (ushort)CurrentSeqNr, new PlayfieldLoad( [float nSecs], [string nPlayfield], [int nProcessId] ));
                        PlayfieldLoad Received_PlayfieldLoaded = (PlayfieldLoad)data;
                        PlayfieldProcessIDDict[Received_PlayfieldLoaded.playfield] = Received_PlayfieldLoaded.processId;
                        break;


                    case CmdId.Event_Playfield_Unloaded:
                        //Triggered when there are no players left in a playfield
                        PlayfieldLoad Received_PlayfieldUnLoaded = (PlayfieldLoad)data;
                        PlayfieldProcessIDDict.Remove(Received_PlayfieldUnLoaded.playfield);
                        break;


                    case CmdId.Event_Faction_Changed:
                        //Triggered when an Entity (player too?) changes faction
                        FactionChangeInfo Received_FactionChange = (FactionChangeInfo)data;
                        break;


                    case CmdId.Event_Statistics:
                        //Triggered on various game events like: Player Death, Entity Power on/off, Remove/Add Core
                        StatisticsParam Received_EventStatistics = (StatisticsParam)data;
                        break;


                    case CmdId.Event_Player_DisconnectedWaiting:
                        //Triggered When a player is having trouble logging into the server
                        Id Received_PlayerDisconnectedWaiting = (Id)data;
                        break;


                    case CmdId.Event_TraderNPCItemSold:
                        //Triggered when a player buys an item from a trader
                        TraderNPCItemSoldInfo Received_TraderNPCItemSold = (TraderNPCItemSoldInfo)data;
                        break;


                    case CmdId.Event_Player_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_List, (ushort)CurrentSeqNr, null));
                        IdList Received_PlayerList = (IdList)data;
                        break;


                    case CmdId.Event_Player_Info:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_Info, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        PlayerInfo Received_PlayerInfo = (PlayerInfo)data;
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData RetrievedData = SeqNrStorage[seqNr];
                            if (RetrievedData.Requested == "PlayerInfo" && RetrievedData.function == "ClaimVesselAtPlayer" && Convert.ToString(Received_PlayerInfo.entityId) == RetrievedData.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                                RetrievedData.Requested = "NewEntityID";
                                RetrievedData.TriggerPlayer = Received_PlayerInfo;
                                API.CreateEntityID(RetrievedData);
                            }
                            else if (RetrievedData.Requested == "PlayerInfo" && RetrievedData.function == "ClaimVesselStatic" && Convert.ToString(Received_PlayerInfo.entityId) == RetrievedData.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                                RetrievedData.Requested = "NewEntityID";
                                RetrievedData.TriggerPlayer = Received_PlayerInfo;
                                API.CreateEntityID(RetrievedData);
                            }
                            else if (RetrievedData.Requested == "PlayerInfo" && RetrievedData.function == "HealMe" && Convert.ToString(Received_PlayerInfo.entityId) == RetrievedData.Match)
                            {
                                SeqNrStorage.Remove(seqNr);
                                //foreach stat in list... do the thing
                                if (File.Exists(MyEmpyrionMod.ModPath + "VoteRewards\\" + Received_PlayerInfo.steamId + ".yaml"))
                                {
                                    VoteRewardYaml.Root PlayerRewards = VoteRewardYaml.ReadYaml(ModPath + "VoteRewards\\" + Received_PlayerInfo.steamId + ".yaml");
                                    if (PlayerRewards.HealMe > 0)
                                    {
                                        foreach (string effect in SetupYamlData.VoteReward.HealMeEffects)
                                        {
                                            bool StatBarAffected = false;
                                            PlayerInfoSet Change = new PlayerInfoSet
                                            {
                                                entityId = Received_PlayerInfo.entityId
                                            };
                                            if ( effect == "FullHealth")
                                            {
                                                StatBarAffected = true;
                                                Change.health = Convert.ToInt32(Math.Round(Received_PlayerInfo.healthMax));
                                                API.ServerTell(Received_PlayerInfo.entityId, "HealMe", "Health set to " + Received_PlayerInfo.healthMax, false);
                                            }
                                            else if ( effect == "FullOxygen")
                                            {
                                                StatBarAffected = true;
                                                Change.oxygenMax = Convert.ToInt32(Math.Round(Received_PlayerInfo.oxygenMax));
                                                API.ServerTell(Received_PlayerInfo.entityId, "HealMe", "Oxygen set to " + Received_PlayerInfo.oxygenMax, false);
                                            }
                                            else if (effect == "FullStamina")
                                            {
                                                StatBarAffected = true;
                                                Change.stamina = Convert.ToInt32(Math.Round(Received_PlayerInfo.staminaMax));
                                                API.ServerTell(Received_PlayerInfo.entityId, "HealMe", "Stamina set to " + Received_PlayerInfo.staminaMax, false);
                                            }
                                            else if (effect == "FullFood")
                                            {
                                                StatBarAffected = true;
                                                Change.food = Convert.ToInt32(Math.Round(Received_PlayerInfo.foodMax));
                                                API.ServerTell(Received_PlayerInfo.entityId, "HealMe", "Food set to " + Received_PlayerInfo.foodMax, false);
                                            }
                                            else if (effect == "ZeroRadiation")
                                            {
                                                StatBarAffected = true;
                                                Change.radiation = 0;
                                                API.ServerTell(Received_PlayerInfo.entityId, "HealMe", "Radiation set to 0", false);
                                            }
                                            else if (effect == "ResetTemperature")
                                            {
                                                StatBarAffected = true;
                                                Change.bodyTemp = 30;
                                                API.ServerTell(Received_PlayerInfo.entityId, "HealMe", "BodyTemp set to 30c", false);
                                            }
                                            else if (effect == "ArmorRestore")
                                            {
                                                API.ConsoleCommand("remoteex cl=" + Received_PlayerInfo.clientId + " /'armor repairfull /' ");
                                                API.ServerTell(Received_PlayerInfo.entityId, "HealMe", "Armor Restored", false);
                                            }
                                            else if (effect == "ArmorRepair")
                                            {
                                                API.ConsoleCommand("remoteex cl=" + Received_PlayerInfo.clientId + " /'armor repair /' ");
                                                API.ServerTell(Received_PlayerInfo.entityId, "HealMe", "Armor Repaired", false);
                                            }
                                            if (StatBarAffected)
                                            {
                                                API.PlayerInfoChange(Change);
                                            }
                                        }
                                        PlayerRewards.HealMe = PlayerRewards.HealMe - 1;
                                        VoteRewardYaml.WriteYaml(ModPath + "VoteRewards\\" + Received_PlayerInfo.steamId + ".yaml", PlayerRewards);
                                        API.Alert(Received_PlayerInfo.entityId, "You have been healed.", "Blue", 3);
                                    }
                                    else
                                    {
                                        API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "You have no more " + SetupYamlData.VoteReward.HealMeCommand + "/'s left.", true);
                                    }
                                }
                                else
                                {
                                    API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "No votes have yet been recorded for your account.", true);
                                }
                            }
                        }

                        break;


                    case CmdId.Event_Player_Inventory:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_GetInventory, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        Inventory Received_PlayerInventory = (Inventory)data;
                        break;


                    case CmdId.Event_Player_ItemExchange:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_ItemExchange, (ushort)CurrentSeqNr, new ItemExchangeInfo( [id], [title], [description], [buttontext], [ItemStack[]] ));
                        ItemExchangeInfo Received_ItemExchangeInfo = (ItemExchangeInfo)data;
                        //*** on close save inventory into VB/playersdata/[steamID]/Cache.txt
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData IERetrievedData = SeqNrStorage[seqNr];
                            if (IERetrievedData.Requested == "ItemExchange" && IERetrievedData.function == "SlashClaim" && Convert.ToString(Received_ItemExchangeInfo.id) == IERetrievedData.Match)
                            {
                                string ItemExchangeClose_SteamID = modApi.Application.GetPlayerDataFor(Received_ItemExchangeInfo.id).Value.SteamId;
                                try
                                {
                                    if (Directory.Exists(ModFolder + "\\VirtualBackpack"))
                                    {
                                        if (Directory.Exists(ModFolder + "\\VirtualBackpack\\PlayersData"))
                                        {
                                            if (!Directory.Exists(ModFolder + "\\VirtualBackpack\\PlayersData\\" + ItemExchangeClose_SteamID))
                                            {
                                                Directory.CreateDirectory(ModFolder + "\\VirtualBackpack\\PlayersData\\" + ItemExchangeClose_SteamID);
                                            }
                                            foreach (ItemStack IS in Received_ItemExchangeInfo.items)
                                            {
                                                if (!Directory.Exists(ModFolder + "\\VirtualBackpack\\PlayersData\\" + ItemExchangeClose_SteamID))
                                                {
                                                    Directory.CreateDirectory(ModFolder + "\\VirtualBackpack\\PlayersData\\" + ItemExchangeClose_SteamID);
                                                }
                                                File.AppendAllText(ModFolder + "\\VirtualBackpack\\PlayersData\\" + ItemExchangeClose_SteamID + "\\Cache.txt", IS.slotIdx + "," + IS.id + "," + IS.count + "," + IS.decay + "," + IS.ammo + "\r\n");
                                                //CommonFunctions.LogFile(ModFolder + "\\VirtualBackpack\\PlayersData\\" + ItemExchangeClose_SteamID + "\\Cache.txt", IS.slotIdx + "," + IS.id + "," + IS.count + "," + IS.decay + "," + IS.ammo);
                                            }
                                        }
                                        else
                                        {
                                            CommonFunctions.ERROR("ERROR: VirtualBackpack directory is missing the PlayersData Folder, cannot dump unclaimed items into Cache file.");
                                        }
                                    }
                                    else
                                    {
                                        CommonFunctions.ERROR("ERROR: VirtualBackpack not installed, cannot dump unclaimed items into Cache file.");
                                    }
                                }
                                catch
                                {
                                    CommonFunctions.ERROR("ERROR: Something went wrong when ItemExchange Window was closed, cannot dump unclaimed items into Cache file.");
                                }
                            }
                        }
                        break;


                    case CmdId.Event_DialogButtonIndex:
                        //All of This is a Guess
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_ShowDialog_SinglePlayer, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        IdAndIntValue Received_DialogButtonIndex = (IdAndIntValue)data;
                        //Save/Pos = 0, Close/Cancel/Neg = 1
                        break;


                    case CmdId.Event_Player_Credits:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_Credits, (ushort)CurrentSeqNr, new Id( [PlayerID] ));
                        IdCredits Received_PlayerCredits = (IdCredits)data;
                        break;


                    case CmdId.Event_Player_GetAndRemoveInventory:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_GetAndRemoveInventory, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        Inventory Received_PlayerGetRemoveInventory = (Inventory)data;
                        break;


                    case CmdId.Event_Playfield_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_List, (ushort)CurrentSeqNr, null));
                        PlayfieldList Received_PlayfieldList = (PlayfieldList)data;
                        break;


                    case CmdId.Event_Playfield_Stats:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_Stats, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        PlayfieldStats Received_PlayfieldStats = (PlayfieldStats)data;
                        break;


                    case CmdId.Event_Playfield_Entity_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_Entity_List, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        PlayfieldEntityList Received_PlayfieldEntityList = (PlayfieldEntityList)data;
                        break;


                    case CmdId.Event_Dedi_Stats:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Dedi_Stats, (ushort)CurrentSeqNr, null));
                        DediStats Received_DediStats = (DediStats)data;
                        break;


                    case CmdId.Event_GlobalStructure_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GlobalStructure_List, (ushort)CurrentSeqNr, null));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GlobalStructure_Update, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        GlobalStructureList Received_GlobalStructureList = (GlobalStructureList)data;
                        //foreach (GlobalStructureInfo item in Structs.globalStructures[storedInfo[seqNr].PlayerInfo.playfield])
                        break;


                    case CmdId.Event_Entity_PosAndRot:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_PosAndRot, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        IdPositionRotation Received_EntityPosRot = (IdPositionRotation)data;
                        break;


                    case CmdId.Event_Get_Factions:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Get_Factions, (ushort)CurrentSeqNr, new Id( [int] )); //Requests all factions from a certain Id onwards. If you want all factions use Id 1.
                        FactionInfoList Received_FactionInfoList = (FactionInfoList)data;
                        break;


                    case CmdId.Event_NewEntityId:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_NewEntityId, (ushort)CurrentSeqNr, null));
                        Id Request_NewEntityId = (Id)data;
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData RetrievedData = SeqNrStorage[seqNr];
                            //CommonFunctions.Debug();
                            if (RetrievedData.Requested == "NewEntityID" && RetrievedData.function == "ClaimVesselAtPlayer" && NewEntityIDQueue.Contains(RetrievedData.Match))
                            {
                                NewEntityIDQueue.Remove(RetrievedData.Match);
                                RetrievedData.NewEntityId = Request_NewEntityId;
                                PVector3 editedPos = new PVector3
                                {
                                    x = RetrievedData.TriggerPlayer.pos.x + KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].Location.Offset[0],
                                    y = RetrievedData.TriggerPlayer.pos.y + KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].Location.Offset[1],
                                    z = RetrievedData.TriggerPlayer.pos.z + KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].Location.Offset[2]
                                };
                                API.CreatePrefabForPlayer(Request_NewEntityId.id, RetrievedData.TriggerPlayer.playfield, editedPos, RetrievedData.TriggerPlayer.rot, null, KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].EntityType, KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].BlueprintName, RetrievedData.TriggerPlayer.entityId);
                                
                                List<string> UnclaimedCodes = File.ReadAllLines(ModPath + "KeysFiles\\" + KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].KeysFile).ToList();
                                UnclaimedCodes.Remove(RetrievedData.API2DialogBoxData.inputContent);
                                File.WriteAllLines(ModPath + "KeysFiles\\" + KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].KeysFile, UnclaimedCodes);
                                //string SteamID = DB.SteamID(RetrievedData.TriggerPlayer.entityId);
                                PlayerData? PlayerData = modApi.Application.GetPlayerDataFor(RetrievedData.TriggerPlayer.entityId);
                                string SteamID = PlayerData.Value.SteamId;

                                string newTimestamp = File.GetLastWriteTime(ModPath + "KeysFiles\\" + KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].KeysFile).ToString();
                                string FileVersion = File.GetLastWriteTime(ModPath + "KeysFiles\\" + KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].ItemPackFile).ToString();
                                CommonFunctions.LogFile("Logs\\" + Timestamp + ".txt", newTimestamp + " Code=" + RetrievedData.API2DialogBoxData.inputContent + "  Type=" + KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].ClaimableType + "  File=" + KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].ItemPackFile + "  Player=" + SteamID + "  Fileversion=" + FileVersion);

                                Insurance.InsuredEntity newInsuredVessel = new Insurance.InsuredEntity
                                {
                                    BlueprintName = KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].BlueprintName,
                                    EntityID = Request_NewEntityId.id,
                                    InitialSpawnDate = newTimestamp,
                                    InsuranceRemaining = KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].Insurance
                                };

                                if (File.Exists(ModPath + "Insurance\\" + SteamID + ".yaml"))
                                {
                                    Insurance.Root InsuranceYaml = Insurance.ReadYaml(ModPath + "Insurance\\" + SteamID + ".yaml");
                                    InsuranceYaml.InsuredEntities.Add(newInsuredVessel);
                                    Insurance.WriteYaml(ModPath + "Insurance\\" + SteamID + ".yaml", InsuranceYaml);
                                }
                                else
                                {
                                    List<Insurance.InsuredEntity> InsuredList = new List<Insurance.InsuredEntity>
                                    {
                                        newInsuredVessel
                                    };
                                    Insurance.Root InsuranceYaml = new Insurance.Root
                                    {
                                        InsuredEntities = InsuredList
                                    };
                                    Insurance.WriteYaml(ModPath + "Insurance\\" + SteamID + ".yaml", InsuranceYaml);
                                }
                                KeysDictionary.Remove(RetrievedData.API2DialogBoxData.inputContent);
                            }
                            else if (RetrievedData.Requested == "NewEntityID" && RetrievedData.function == "ClaimVesselStatic" && NewEntityIDQueue.Contains(RetrievedData.Match))
                            {
                                NewEntityIDQueue.Remove(RetrievedData.Match);
                                RetrievedData.NewEntityId = Request_NewEntityId;
                                PVector3 editedPos = new PVector3
                                {
                                    x = KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].Location.Coordinates[0],
                                    y = KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].Location.Coordinates[1],
                                    z = KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].Location.Coordinates[2]
                                };
                                PVector3 editedRot = new PVector3
                                {
                                    x = KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].Location.Facing[0],
                                    y = KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].Location.Facing[1],
                                    z = KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].Location.Facing[2]
                                };
                                API.CreatePrefabForPlayer(Request_NewEntityId.id, RetrievedData.TriggerPlayer.playfield, editedPos, editedRot, null, KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].EntityType, KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].BlueprintName, RetrievedData.TriggerPlayer.entityId);

                                List<string> UnclaimedCodes = File.ReadAllLines(ModPath + "KeysFiles\\" + KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].KeysFile).ToList();
                                UnclaimedCodes.Remove(RetrievedData.API2DialogBoxData.inputContent);
                                File.WriteAllLines(ModPath + "KeysFiles\\" + KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].KeysFile, UnclaimedCodes);
                                //string SteamID = DB.SteamID(RetrievedData.TriggerPlayer.entityId);
                                PlayerData? PlayerData = modApi.Application.GetPlayerDataFor(RetrievedData.TriggerPlayer.entityId);
                                string SteamID = PlayerData.Value.SteamId;

                                string newTimestamp = File.GetLastWriteTime(ModPath + "KeysFiles\\" + KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].KeysFile).ToString();
                                string FileVersion = File.GetLastWriteTime(ModPath + "KeysFiles\\" + KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].ItemPackFile).ToString();
                                CommonFunctions.LogFile("Logs\\" + Timestamp + ".txt", newTimestamp + " Code=" + RetrievedData.API2DialogBoxData.inputContent + "  Type=" + KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].ClaimableType + "  File=" + KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].ItemPackFile + "  Player=" + SteamID + "  Fileversion=" + FileVersion);

                                Insurance.InsuredEntity newInsuredVessel = new Insurance.InsuredEntity
                                {
                                    BlueprintName = KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].BlueprintName,
                                    EntityID = Request_NewEntityId.id,
                                    InitialSpawnDate = newTimestamp,
                                    InsuranceRemaining = KeysDictionary[RetrievedData.API2DialogBoxData.inputContent].Insurance
                                };

                                if (File.Exists(ModPath + "Insurance\\" + SteamID + ".yaml"))
                                {
                                    Insurance.Root InsuranceYaml = Insurance.ReadYaml(ModPath + "Insurance\\" + SteamID + ".yaml");
                                    InsuranceYaml.InsuredEntities.Add(newInsuredVessel);
                                    Insurance.WriteYaml(ModPath + "Insurance\\" + SteamID + ".yaml", InsuranceYaml);
                                }
                                else
                                {
                                    List<Insurance.InsuredEntity> InsuredList = new List<Insurance.InsuredEntity>
                                    {
                                        newInsuredVessel
                                    };
                                    Insurance.Root InsuranceYaml = new Insurance.Root
                                    {
                                        InsuredEntities = InsuredList
                                    };
                                    Insurance.WriteYaml(ModPath + "Insurance\\" + SteamID + ".yaml", InsuranceYaml);
                                }
                                KeysDictionary.Remove(RetrievedData.API2DialogBoxData.inputContent);
                            }
                        }
                        break;


                    case CmdId.Event_Structure_BlockStatistics:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Structure_BlockStatistics, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        IdStructureBlockInfo Received_StructureBlockStatistics = (IdStructureBlockInfo)data;
                        break;


                    case CmdId.Event_AlliancesAll:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_AlliancesAll, (ushort)CurrentSeqNr, null));
                        AlliancesTable Received_AlliancesAll = (AlliancesTable)data;
                        break;


                    case CmdId.Event_AlliancesFaction:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_AlliancesFaction, (ushort)CurrentSeqNr, new AlliancesFaction( [int nFaction1Id], [int nFaction2Id], [bool nIsAllied] ));
                        AlliancesFaction Received_AlliancesFaction = (AlliancesFaction)data;
                        break;


                    case CmdId.Event_BannedPlayers:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GetBannedPlayers, (ushort)CurrentSeqNr, null ));
                        BannedPlayerData Received_BannedPlayers = (BannedPlayerData)data;
                        break;


                    case CmdId.Event_GameEvent:
                        //Triggered by PDA Events
                        GameEventData Received_GameEvent = (GameEventData)data;
                        
                        break;


                    case CmdId.Event_Ok:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_SetInventory, (ushort)CurrentSeqNr, new Inventory(){ [changes to be made] });
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_AddItem, (ushort)CurrentSeqNr, new IdItemStack(){ [changes to be made] });
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_SetCredits, (ushort)CurrentSeqNr, new IdCredits( [PlayerID], [Double] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_AddCredits, (ushort)CurrentSeqNr, new IdCredits( [PlayerID], [+/- Double] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Blueprint_Finish, (ushort)CurrentSeqNr, new Id( [PlayerID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Blueprint_Resources, (ushort)CurrentSeqNr, new BlueprintResources( [PlayerID], [List<ItemStack>], [bool ReplaceExisting?] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Teleport, (ushort)CurrentSeqNr, new IdPositionRotation( [EntityId OR PlayerID], [Pvector3 Position], [Pvector3 Rotation] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_ChangePlayfield , (ushort)CurrentSeqNr, new IdPlayfieldPositionRotation( [EntityId OR PlayerID], [Playfield],  [Pvector3 Position], [Pvector3 Rotation] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Destroy, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Destroy2, (ushort)CurrentSeqNr, new IdPlayfield( [EntityID], [Playfield] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_SetName, (ushort)CurrentSeqNr, new Id( [EntityID] )); Wait, what? This one doesn't make sense. This is what the Wiki says though.
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Spawn, (ushort)CurrentSeqNr, new EntitySpawnInfo()); Doesn't make sense to me.
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Structure_Touch, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_SinglePlayer, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_Faction, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_AllPlayers, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_ConsoleCommand, (ushort)CurrentSeqNr, new PString( [Telnet Command] ));

                        //uh? Not Listed in Wiki... Received_ = ()data;
                        break;


                    case CmdId.Event_Error:
                        //Triggered when there is an error coming from the API
                        ErrorInfo Received_ErrorInfo = (ErrorInfo)data;
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            CommonFunctions.ERROR("API Error:");
                            CommonFunctions.ERROR("ErrorType: " + Received_ErrorInfo.errorType);
                            CommonFunctions.ERROR("");
                        }
                        break;


                    case CmdId.Event_PdaStateChange:
                        //Triggered by PDA: chapter activated/deactivated/completed
                        PdaStateInfo Received_PdaStateChange = (PdaStateInfo)data;
                        break;


                    case CmdId.Event_ConsoleCommand:
                        //Triggered when a player uses a Console Command in-game
                        ConsoleCommandInfo Received_ConsoleCommandInfo = (ConsoleCommandInfo)data;
                        break;


                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                CommonFunctions.ERROR("Message: " + ex.Message);
                CommonFunctions.ERROR("Data: " + ex.Data);
                CommonFunctions.ERROR("HelpLink: " + ex.HelpLink);
                CommonFunctions.ERROR("InnerException: " + ex.InnerException);
                CommonFunctions.ERROR("Source: " + ex.Source);
                CommonFunctions.ERROR("StackTrace: " + ex.StackTrace);
                CommonFunctions.ERROR("TargetSite: " + ex.TargetSite);
            }
        }

        public void Game_Update()
        {
            //Triggered whenever Empyrion experiences "Downtime", roughly 75-100 times per second
        }
        public void Game_Exit()
        {
            //Triggered when the server is Shutting down. Does NOT pause the shutdown.
        }

        //########################################################################################################################################################
        //################################################ This is where the actual Empyrion Modding API2 stuff Begins ###########################################
        //########################################################################################################################################################

        public void Init(IModApi modApi)
        {
            MyEmpyrionMod.modApi = modApi;
            Timestamp = CommonFunctions.TimeStamp();
            ModPath = modApi.Application.GetPathFor(AppFolder.Mod) + "\\" + ModShortName + "\\";
            string SaveGamePath = modApi.Application.GetPathFor(AppFolder.SaveGame);
            string[] SaveGameArray = SaveGamePath.Split('/');
            SaveGameName = SaveGameArray.Last();
            try
            {
                SetupYaml.Setup();
            }
            catch
            {
                CommonFunctions.ERROR("ERROR: running SetupYaml.Setup() while Initializing failed");
            }

            modApi.Log($"GameTime (ticks): {modApi.Application.GameTicks}");
            ModFolder = modApi.Application.GetPathFor(AppFolder.Mod);
            //modApi.GameEvent += ModApi_GameEvent;
            if (modApi.Application.Mode == ApplicationMode.DedicatedServer)
            {
                modApi.Application.ChatMessageSent += Application_ChatMessageSent;
            }
            else if (modApi.Application.Mode == ApplicationMode.PlayfieldServer)
            {
                modApi.Network.RegisterReceiverForPlayfieldPackets(NetworkFromPfServer);
                modApi.Application.OnPlayfieldLoaded += Application_OnPlayfieldLoaded;
            }
        }

        private void Application_OnPlayfieldLoaded(IPlayfield playfield)
        {
            Playfield = playfield;
            CommonFunctions.Log("--------------------" + Timestamp + "----------------------------");
            playfield.OnEntityLoaded += Playfield_OnEntityLoaded;
        }

        private void Playfield_OnEntityLoaded(IEntity entity)
        {
            if(entity.Type == EntityType.BA || entity.Type == EntityType.CV || entity.Type == EntityType.SV || entity.Type == EntityType.HV || entity.Type == EntityType.Player)
            {
                string SendableString = "EntityData " + entity.Id + " " + CommonFunctions.SanitizeString(entity.Name);
                modApi.Network.SendToDedicatedServer(ModShortName, CommonFunctions.ConvertToByteArray(SendableString), Playfield.Name);
                CommonFunctions.Log(SendableString);
            }
        }

        private void NetworkFromPfServer(string sender, string playfieldName, byte[] data)
        {
            string ReceivedData = CommonFunctions.ConvertByteArrayToString(data);
            if (sender == "SubscriptionVerifier")
            {
                string IncommingData = CommonFunctions.ConvertByteArrayToString(data);
                if (IncommingData.StartsWith("Expiration "))
                {
                    int NewExpiration = int.Parse(IncommingData.Split(' ')[1]);
                    Expiration = NewExpiration;
                    if (Expiration > int.Parse(CommonFunctions.UnixTimeStamp()))
                    {
                        Disable = false;
                    }
                    else
                    {
                        Disable = true;
                    }
                    CommonFunctions.LogFile("SV.txt", "Expiration = " + Expiration);
                    CommonFunctions.LogFile("SV.txt", "Disable = " + Disable);
                }
            }
            else if (ReceivedData.Contains(' '))
            {
                string[] RDsplit = ReceivedData.Split(' ');
                if ( RDsplit[0] == "EntityData")
                {
                    int EntityID = int.Parse(RDsplit[1]);
                    string EntityName = CommonFunctions.ChatmessageHandler(RDsplit, "2*");
                    EntityData LoggableEntity = new EntityData
                    {
                        Playfield = playfieldName,
                        Name = EntityName
                    };
                    EntityDataFromPfServer[EntityID] = LoggableEntity;
                }
            }
        }

        private void Application_ChatMessageSent(MessageData chatMsgData)
        {
            if (Disable)
            {
                API.ServerTell(chatMsgData.SenderEntityId, ModShortName, "Mod is Disabled", false);
            }
            else
            {
                string msg = chatMsgData.Text.ToLower();
                string msg2 = "";
                if (msg.Contains(' '))
                {
                    try
                    {
                        string[] msgArray = msg.Split(' ');
                        msg2 = msgArray[1];
                    }
                    catch { }
                }
                string SteamID = modApi.Application.GetPlayerDataFor(chatMsgData.SenderEntityId).Value.SteamId;
                if (msg.StartsWith(SetupYamlData.General.ClaimCommand))
                {
                    //Open Popup Window, on close check code against KeysDictionary.keys
                    string Claimables = "Your current rewards: \r\n";
                    VoteRewardYaml.Root CurrentRewards = new VoteRewardYaml.Root { };
                    if (File.Exists(ModPath + "VoteRewards\\" + SteamID + ".yaml"))
                    {
                        CurrentRewards = VoteRewardYaml.ReadYaml(ModPath + "VoteRewards\\" + SteamID + ".yaml");
                        if (CurrentRewards.HealMe > 0)
                        {
                            Claimables = Claimables + SetupYamlData.VoteReward.HealMeCommand + " x" + CurrentRewards.HealMe + "\r\n";
                        }
                        if (CurrentRewards.RaffleTickets > 0)
                        {
                            Claimables = Claimables + "RaffleTickets x" + CurrentRewards.RaffleTickets + "\r\n";
                        }
                        if (CurrentRewards.Credits > 0)
                        {
                            Claimables = Claimables + "Credits x" + CurrentRewards.Credits + "\r\n";
                        }
                        if (CurrentRewards.ItemStacks.Count > 0)
                        {
                            Claimables = Claimables + "Items x?\r\n";
                        }
                        if (CurrentRewards.Claimables.Count() > 0)
                        {
                            Claimables = Claimables + "\r\nItem Packs, Vessels and Bases" + "\r\n";
                            foreach (SetupYaml.Claimable Claimable in CurrentRewards.Claimables)
                            {
                                Claimables = Claimables + Claimable.Code + "\r\n";
                            }
                        }
                    }
                    Claimables = Claimables + "\r\nFor Website Purchases: Type the code you received in the box below to claim your reward.";
                    DialogConfig newDialog = new DialogConfig
                    {
                        TitleText = "Claimables",
                        BodyText = Claimables,
                        ButtonIdxForEnter = 0,
                        ButtonIdxForEsc = 1,
                        ButtonTexts = new string[2] { "Claim", "Cancel" },
                        CloseOnLinkClick = false,
                        InitialContent = "",
                        MaxChars = 1000,
                        Placeholder = "Type Code Here to claim"
                    };
                    CommonFunctions.Debug("before set msg2");
                    if (msg.Contains(' ')) newDialog.InitialContent = msg2;
                    CommonFunctions.Debug("after set msg2");
                    DialogActionHandler DialogHandler = new DialogActionHandler(OnDialogBoxClosed);
                    modApi.Application.ShowDialogBox(chatMsgData.SenderEntityId, newDialog, DialogHandler, 10);
                }
                else if (msg.ToLower().Contains(SetupYamlData.General.InsuranceCommand.ToLower()))
                {
                    //Check to see if the vessel is insured FIRST
                    //string SteamID = DB.SteamID(Received_ChatInfo.playerId);
                    PlayerData? PlayerData = modApi.Application.GetPlayerDataFor(chatMsgData.SenderEntityId);
                    //string SteamID = PlayerData.Value.SteamId;

                    string BodyText = "Warning: Vessel Inventory will be deleted.\r\n Which of these vessels do you wish to file a claim on?\r\n";
                    try
                    {
                        Insurance.Root InsuranceYaml = Insurance.ReadYaml(ModPath + "Insurance\\" + SteamID + ".yaml");
                        foreach (Insurance.InsuredEntity Entity in InsuranceYaml.InsuredEntities)
                        {
                            try
                            {

                                //DB.EntityData EntityData = DB.LookupEntity(Entity.EntityID);
                                string EntityName = EntityDataFromPfServer[Entity.EntityID].Name;
                                if (Entity.InsuranceRemaining > 0)
                                {
                                    BodyText = BodyText + Entity.EntityID + "  " + EntityName + "\r\n";
                                }
                            }
                            catch { }
                        }
                        DialogConfig newDialog = new DialogConfig
                        {
                            TitleText = "Claim Insurance",
                            BodyText = BodyText,
                            ButtonIdxForEnter = 0,
                            ButtonIdxForEsc = 1,
                            ButtonTexts = new string[2] { "File", "Cancel" },
                            CloseOnLinkClick = false,
                            InitialContent = "",
                            MaxChars = 20,
                            Placeholder = "Type Vessel ID"
                        };
                        if (msg.Contains(' ')) newDialog.InitialContent = msg2;
                        DialogActionHandler DialogHandler = new DialogActionHandler(ClaimInsurance);
                        modApi.Application.ShowDialogBox(chatMsgData.SenderEntityId, newDialog, DialogHandler, 10);
                    }
                    catch
                    {
                        API.ServerTell(chatMsgData.SenderEntityId, ModShortName, "You Don\'t have any insurance policies.", true);
                    }

                }
                else if (msg.ToLower().Contains(SetupYamlData.VoteReward.Command.ToLower()))
                {
                    Task returned = RestCheckVote(chatMsgData.SenderEntityId);
                }
                else if (msg.ToLower().Contains(SetupYamlData.VoteReward.HealMeCommand.ToLower()))
                {
                    Storage.StorableData StorableData = new Storage.StorableData
                    {
                        function = "HealMe",
                        Match = Convert.ToString(chatMsgData.SenderEntityId),
                        Requested = "PlayerInfo",
                        ChatMessageData = chatMsgData
                        //ChatInfo = Received_ChatInfo
                    };
                    API.PlayerInfo(chatMsgData.SenderEntityId, StorableData);
                }
            }
        }

        static void OnDialogBoxClosed(int buttonIdx, string linkID, string inputContent, int PlayerID, int CustomValue)
        {
            CommonFunctions.Debug("input Content = " + inputContent);
            CommonFunctions.Debug("buttonIdx = " + buttonIdx);
            CommonFunctions.Debug("LinkID = " + linkID);
            CommonFunctions.Debug("PlayerID " + PlayerID);
            CommonFunctions.Debug("Custom Value = " + CustomValue);
            CommonFunctions.Debug("");

            if (buttonIdx == 0 && inputContent.ToLower() == "credits")
            {
                string SteamID = modApi.Application.GetPlayerDataFor(PlayerID).Value.SteamId;
                if (File.Exists(MyEmpyrionMod.ModPath + "VoteRewards\\" + SteamID + ".yaml"))
                {
                    VoteRewardYaml.Root VoteRewards = VoteRewardYaml.ReadYaml(MyEmpyrionMod.ModPath + "VoteRewards\\" + SteamID + ".yaml");
                    if (VoteRewards.Credits.HasValue)
                    {
                        API.Credits(PlayerID, Convert.ToInt32(Math.Round(VoteRewards.Credits.Value, 0)));
                        VoteRewards.Credits = 0;
                        VoteRewardYaml.WriteYaml(MyEmpyrionMod.ModPath + "VoteRewards\\" + SteamID + ".yaml", VoteRewards);
                    }
                }
            }
            else if (buttonIdx == 0 && inputContent.ToLower() == "items")
            {
                string Message = "";
                try
                {
                    string SteamID = modApi.Application.GetPlayerDataFor(PlayerID).Value.SteamId;
                    if (File.Exists(MyEmpyrionMod.ModPath + "VoteRewards\\" + SteamID + ".yaml"))
                    {
                        VoteRewardYaml.Root VoteRewards = VoteRewardYaml.ReadYaml(MyEmpyrionMod.ModPath + "VoteRewards\\" + SteamID + ".yaml");
                        Dictionary<int, VoteRewardYaml.ItemStacks> ItemDictionary = new Dictionary<int, VoteRewardYaml.ItemStacks> { };
                        foreach (VoteRewardYaml.ItemStacks item in VoteRewards.ItemStacks)
                        {
                            if (ItemDictionary.Keys.Contains(item.ItemID))
                            {
                                ItemDictionary[item.ItemID].Quantity = ItemDictionary[item.ItemID].Quantity + item.Quantity;
                            }
                            else
                            {
                                ItemDictionary.Add(item.ItemID, item);
                            }
                        }
                        List<ItemStack> ItemStackList = new List<ItemStack> { };
                        foreach (VoteRewardYaml.ItemStacks stack in ItemDictionary.Values)
                        {
                            ItemStack newStack = new ItemStack
                            {
                                slotIdx = 0,
                                id = stack.ItemID,
                                count = stack.Quantity
                            };
                            if(stack.Ammo.HasValue)
                            {
                                newStack.ammo = stack.Ammo.Value;
                            }
                            if (stack.Decay.HasValue)
                            {
                                newStack.decay = stack.Decay.Value;
                            }
                            ItemStackList.Add(newStack)
;                        }
                        if (ItemStackList.Count == 0)
                        {
                            API.ServerTell(PlayerID, "Xango2000", "Yes, I thought somone might figure that exploit out... fixed it", true);
                        }
                        else
                        {
                            Storage.StorableData StoreMe = new Storage.StorableData
                            {
                                Match = PlayerID.ToString(),
                                Requested = "ItemExchange",
                                function = "SlashClaim"
                            };
                            API.OpenItemExchange(PlayerID, "VoteRewards", "Anything not claimed will be deleted.", "Close", ItemStackList.ToArray(), StoreMe);
                        }
                        VoteRewards.ItemStacks = new List<VoteRewardYaml.ItemStacks> { };
                        VoteRewardYaml.WriteYaml(MyEmpyrionMod.ModPath + "VoteRewards\\" + SteamID + ".yaml", VoteRewards);
                    }
                }
                catch
                {
                    Message = inputContent + " Not Found";
                }
                API.ServerTell(PlayerID, ModShortName, Message, true);

            }
            else if ( buttonIdx == 0 && KeysDictionary.Keys.Contains(inputContent))
            {
                //buttonIdx=0 means "Claim and/or Enter, buttonIdx=1 means "Cancel and/or Esc". In This mod.
                if ( KeysDictionary[inputContent].ClaimableType.ToLower() == "itempack")
                {
                    //if Claimable type == ItemPack then request ItemExchange with Items using ItemsFileName
                    try
                    {
                        ItemStack[] ItemPackData = CommonFunctions.ReadItemStacks("ItemPacks\\", KeysDictionary[inputContent].ItemPackFile);
                        Storage.StorableData StorableData = new Storage.StorableData
                        {
                            function = "ClaimItemPack",
                            Match = Convert.ToString(PlayerID),
                            Requested = "ItemExchange"
                        };
                        API.OpenItemExchange(PlayerID, "ClaimCode", "Please collect all your belongings.", "Close", ItemPackData, StorableData);

                        List<string> UnclaimedCodes = File.ReadAllLines(ModPath + "KeysFiles\\" + KeysDictionary[inputContent].KeysFile).ToList();
                        UnclaimedCodes.Remove(inputContent);
                        File.WriteAllLines(ModPath + "KeysFiles\\" + KeysDictionary[inputContent].KeysFile, UnclaimedCodes);
                        string SteamID = null;
                        try
                        {
                            //SteamID = DB.SteamID(PlayerID);
                            PlayerData? PlayerData = modApi.Application.GetPlayerDataFor(PlayerID);
                            SteamID = PlayerData.Value.SteamId;
                        }
                        catch
                        {
                            API.ServerTell(PlayerID, ModShortName, "Unable to find database or database entry. Unable to log /claim.", true);
                        }
                        if ( SteamID != null)
                        {
                            string newTimestamp = File.GetLastWriteTime(ModPath + "KeysFiles\\" + KeysDictionary[inputContent].KeysFile).ToString();
                            string FileVersion = File.GetLastWriteTime(ModPath + "KeysFiles\\" + KeysDictionary[inputContent].ItemPackFile).ToString();
                            CommonFunctions.LogFile("Logs\\" + Timestamp + ".txt", newTimestamp + " Code=" + inputContent + "  Type=" + KeysDictionary[inputContent].ClaimableType + "  File=" + KeysDictionary[inputContent].ItemPackFile + "  Player=" + SteamID + "  Fileversion=" + FileVersion);
                        }
                        KeysDictionary.Remove(inputContent);
                    }
                    catch
                    {
                        API.ServerTell(PlayerID, ModShortName, "ItemPack does not exist.", true);
                    }
                }
                else if (KeysDictionary[inputContent].ClaimableType.ToLower() == "vessel" && KeysDictionary[inputContent].Location.Type.ToLower() == "atplayer")
                {
                    Storage.API2DialogBoxDelegateData DialogData = new Storage.API2DialogBoxDelegateData
                    {
                        buttonIdx = buttonIdx,
                        CustomValue = CustomValue,
                        inputContent = inputContent,
                        linkID = linkID,
                        PlayerID = PlayerID
                    };
                    NewEntityIDQueue.Add(PlayerID.ToString());
                    Storage.StorableData StorableData = new Storage.StorableData
                    {
                        function = "ClaimVesselAtPlayer",
                        Match = Convert.ToString(PlayerID),
                        Requested = "PlayerInfo",
                        API2DialogBoxData = DialogData
                    };
                    API.PlayerInfo(PlayerID, StorableData);
                }
                else if (KeysDictionary[inputContent].ClaimableType.ToLower() == "vessel" && KeysDictionary[inputContent].Location.Type.ToLower() == "staticlocation")
                {
                    Storage.API2DialogBoxDelegateData DialogData = new Storage.API2DialogBoxDelegateData
                    {
                        buttonIdx = buttonIdx,
                        CustomValue = CustomValue,
                        inputContent = inputContent,
                        linkID = linkID,
                        PlayerID = PlayerID
                    };
                    NewEntityIDQueue.Add(PlayerID.ToString());
                    Storage.StorableData StorableData = new Storage.StorableData
                    {
                        function = "ClaimVesselStatic",
                        Match = Convert.ToString(PlayerID),
                        Requested = "PlayerInfo",
                        API2DialogBoxData = DialogData
                    };
                    API.PlayerInfo(PlayerID, StorableData);
                }
                else
                {
                    API.ServerTell(PlayerID, ModShortName, "Invalid Setup Detected, please contact Admin.", true);
                }
            }
            else if (buttonIdx == 0)
            {
                //This is the code for Voterewards, if Input is a NickName
                //OnDialogBoxClosed(int buttonIdx, string linkID, string inputContent, int PlayerID, int CustomValue)
                string Message = "";
                try
                {
                    string SteamID = modApi.Application.GetPlayerDataFor(PlayerID).Value.SteamId;
                    if (File.Exists(MyEmpyrionMod.ModPath + "VoteRewards\\" + SteamID + ".yaml"))
                    {
                        VoteRewardYaml.Root VoteRewards = VoteRewardYaml.ReadYaml(MyEmpyrionMod.ModPath + "VoteRewards\\" + SteamID + ".yaml");
                        for (int i = 0; i < VoteRewards.Claimables.Count; i++)
                        //foreach (SetupYaml.Claimable Claimable in VoteRewards.Claimables)
                        {
                            if (VoteRewards.Claimables[i].Code == inputContent)
                            {
                                SomethingClaimable.ClaimReward(PlayerID, VoteRewards.Claimables[i]);
                                VoteRewards.Claimables.RemoveAt(i);
                                VoteRewardYaml.WriteYaml(MyEmpyrionMod.ModPath + "VoteRewards\\" + SteamID + ".yaml", VoteRewards);
                                Message = "Claimed: " + VoteRewards.Claimables[i].Code;
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    Message =  inputContent + " Not Found";
                }
                API.ServerTell(PlayerID, ModShortName, Message, true);
            }
            else
            {
                //API.ServerTell(PlayerID, ModShortName, "Invalid Code Entered ( " + inputContent + " )", true); This is dumb, Don't do this...
            }
        }
        
        static void ClaimInsurance(int buttonIdx, string linkID, string inputContent, int PlayerID, int CustomValue)
        {
            if (buttonIdx == 0)
            {
                try
                {
                    //string SteamID = DB.SteamID(PlayerID);
                    PlayerData? PlayerData = modApi.Application.GetPlayerDataFor(PlayerID);
                    string SteamID = PlayerData.Value.SteamId;
                    Insurance.Root InsuranceYaml = Insurance.ReadYaml(ModPath + "Insurance\\" + SteamID + ".yaml");
                    List<Insurance.InsuredEntity> newList = new List<Insurance.InsuredEntity> { };
                    foreach (Insurance.InsuredEntity Entity in InsuranceYaml.InsuredEntities)
                    {
                        if (Entity.EntityID.ToString() == inputContent)
                        {
                            Insurance.InsuredEntity editEntity = new Insurance.InsuredEntity
                            {
                                BlueprintName = Entity.BlueprintName,
                                EntityID = Entity.EntityID,
                                InitialSpawnDate = Entity.InitialSpawnDate,
                                InsuranceRemaining = Entity.InsuranceRemaining - 1
                            };
                            newList.Add(editEntity);
                            API.Regenerate(int.Parse(inputContent), PlayfieldProcessIDDict[EntityDataFromPfServer[Entity.EntityID].Playfield]);
                            API.ServerTell(PlayerID, ModShortName, "Insurance Claimed", true);
                        }
                        else
                        {
                            newList.Add(Entity);
                        }
                    }
                    InsuranceYaml.InsuredEntities = newList;
                    Insurance.WriteYaml(ModPath + "Insurance\\" + SteamID + ".yaml", InsuranceYaml);
                }
                catch
                {
                    API.ServerTell(PlayerID, ModShortName, "Something Went Wrong editing insurance file.", false);
                }
            }
        }

        public void Shutdown()
        {
            modApi.Log("Dedi mod shutdown");
        }

        private void ModApi_GameEvent(GameEventType type, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            modApi.Log("Game Event Triggered");
            CommonFunctions.Debug("Type = " + type);
            try
            {
                CommonFunctions.Debug("arg1 type = " + arg1.GetType());
                CommonFunctions.Debug("arg1 = " + arg1);
                CommonFunctions.Debug("arg2 type = " + arg2.GetType());
                CommonFunctions.Debug("arg2 = " + arg2);
                CommonFunctions.Debug("arg3 type = " + arg3.GetType());
                CommonFunctions.Debug("arg3 = " + arg3);
                CommonFunctions.Debug("arg4 type = " + arg4.GetType());
                CommonFunctions.Debug("arg4 = " + arg4);
                CommonFunctions.Debug("arg5 type = " + arg5.GetType());
                CommonFunctions.Debug("arg5 = " + arg5);
            }
            catch { }
            CommonFunctions.Debug("");
        }

        private async Task<string> RestCheckVote(int PlayerID)
        {
            string SteamID = modApi.Application.GetPlayerDataFor(PlayerID).Value.SteamId;
            string url = $"https://empyrion-servers.com/api/?object=votes&element=claim&key={SetupYamlData.VoteReward.APIkey}&steamid={SteamID}";
            string response = await Rest("GET", url);
            CommonFunctions.LogFile("Logs\\" + Timestamp + ".txt", CommonFunctions.TimeStamp() + "   Player=" + PlayerID + "   SteamID=" + SteamID + "   VoteSiteResponse=" + response);
            string[] VotersArray = new string[] { };
            bool ThisMonthFileExisted = false;
            if (File.Exists(ModPath + "Logs\\" + CommonFunctions.TimestampArray()[0] + CommonFunctions.TimestampArray()[1] + ".txt"))
            {
                VotersArray = File.ReadAllLines(ModPath +"Logs\\" + CommonFunctions.TimestampArray()[0] + CommonFunctions.TimestampArray()[1] + ".txt");
                ThisMonthFileExisted = true;
            }
            int VoteCount = 0;
            foreach(string Voter in VotersArray)
            {
                if (Voter == SteamID)
                {
                    VoteCount = VoteCount + 1;
                }
            }
            if (response == "0")
            {
                API.ServerTell(PlayerID, MyEmpyrionMod.ModShortName, "Please vote for our server.", false);
            }
            else if (response == "1")
            {
                CommonFunctions.LogFile("Logs\\" + CommonFunctions.TimestampArray()[0] + CommonFunctions.TimestampArray()[1] + ".txt", SteamID);
                VoteRewardYaml.Root CurrentRewards = new VoteRewardYaml.Root
                {
                    EmpyrionID = PlayerID,
                    Claimables = new List<SetupYaml.Claimable> { },
                    HealMe = 0,
                    Credits = 0,
                    ItemStacks = new List<VoteRewardYaml.ItemStacks> { },
                    RaffleTickets = 0
                };
                if (File.Exists(ModPath + "VoteRewards\\" + SteamID + ".yaml"))
                {
                    CurrentRewards = VoteRewardYaml.ReadYaml(ModPath + "VoteRewards\\" + SteamID + ".yaml");
                }
                foreach (SetupYaml.Reward Reward in SetupYamlData.VoteReward.DailyReward)
                {
                    if (Reward.Type == "ItemStacks")
                    {
                        foreach (VoteRewardYaml.ItemStacks ListedItemStack in Reward.ItemStacks)
                        {
                            int Bonus = Convert.ToInt32(Math.Round((ListedItemStack.Quantity * ListedItemStack.Multiplier * VoteCount)));
                            VoteRewardYaml.ItemStacks NewItem = new VoteRewardYaml.ItemStacks
                            {
                                ItemID = ListedItemStack.ItemID,
                                Quantity = ListedItemStack.Quantity + Bonus,
                                Ammo = ListedItemStack.Ammo,
                                Decay = ListedItemStack.Decay,
                            };
                            CurrentRewards.ItemStacks.Add(NewItem);
                        }
                    }
                    else if (Reward.Type == "Credits")
                    {
                        double Bonus = Reward.Quantity * Reward.Multiplier * VoteCount;
                        if (CurrentRewards.Credits.HasValue)
                        {
                            CurrentRewards.Credits = Math.Round(CurrentRewards.Credits.Value + Reward.Quantity + Bonus, 2);
                            //API.ServerTell(PlayerID, ModShortName, AddCredits + " Credits have been added to your account", false);
                        }
                        else
                        {
                            CurrentRewards.Credits = Math.Round(Reward.Quantity + Bonus, 2);
                        }
                    }
                    else if (Reward.Type == "HealMe")
                    {
                        double Bonus = Reward.Quantity * Reward.Multiplier * VoteCount;
                        if (CurrentRewards.HealMe.HasValue)
                        {
                            CurrentRewards.HealMe = Math.Round(CurrentRewards.HealMe.Value + Reward.Quantity + Bonus, 2);
                        }
                        else
                        {
                            CurrentRewards.HealMe = Math.Round(Reward.Quantity + Bonus, 2);
                        }
                    }
                    else if (Reward.Type == "RaffleTicket")
                    {
                        double Bonus = Reward.Quantity * Reward.Multiplier * VoteCount;
                        if (CurrentRewards.Credits.HasValue)
                        {
                            CurrentRewards.RaffleTickets = Math.Round(CurrentRewards.RaffleTickets.Value + Reward.Quantity + Bonus, 2);
                        }
                        else
                        {
                            CurrentRewards.RaffleTickets = Math.Round(Reward.Quantity + Bonus, 2);
                        }
                    }
                    else if (Reward.Type == "Claimable")
                    {
                        foreach (SetupYaml.Claimable claimable in Reward.Claimable)
                        {
                            CurrentRewards.Claimables.Add(claimable);
                        }
                    }
                }
                
                VoteRewardYaml.WriteYaml(ModPath + "VoteRewards\\" + SteamID + ".yaml", CurrentRewards);
                API.ServerTell(PlayerID, MyEmpyrionMod.ModShortName, "Your vote reward has been added to /claim", false);
                await RestMarkClaimed(SteamID); //***
            }

            if (ThisMonthFileExisted)
            {
                //MonthlyBonusAfterXVotes section goes here
                if (VoteCount == SetupYamlData.VoteReward.MonthlyBonusAfterXVotes)
                {
                    if (File.Exists(ModPath + "VoteRewards\\" + SteamID + ".yaml"))
                    {
                        VoteRewardYaml.Root CurrentRewards = VoteRewardYaml.ReadYaml(ModPath + "VoteRewards\\" + SteamID + ".yaml");
                        SomethingClaimable.GiveReward(SteamID, CurrentRewards, SetupYamlData.VoteReward.MonthlyBonus);
                    }
                }
            }
            else
            {
                //await RestVoteHistory();
                //*** SomethingClaimable.ClaimReward( , SetupYamlData.VoteReward.RaffleReward);
                //*** SomethingClaimable.ClaimReward( , SetupYamlData.VoteReward.TopMonthlyVoterReward);

            }
            return response;
        }

        private async Task RestMarkClaimed(string SteamID)
        {
            string url = $"https://empyrion-servers.com/api/?action=post&object=votes&element=claim&key={SetupYamlData.VoteReward.APIkey}&steamid={SteamID}";
            await Rest("POST", url);
        }
                
        public async static Task<string> Rest(string method, string url)
        {
            WebRequest webrequest = WebRequest.Create(url);
            webrequest.Method = method;
            using (HttpWebResponse response = (HttpWebResponse)await Task.Factory.FromAsync<WebResponse>(webrequest.BeginGetResponse, webrequest.EndGetResponse, null))
            {
                StreamReader responseStream = new StreamReader(response.GetResponseStream(), System.Text.Encoding.GetEncoding("ascii"));
                string result = "";
                result = responseStream.ReadToEnd();
                return result;
            }
        }

        private async Task RestVoteHistory()
        {
            string ServerKey = SetupYamlData.VoteReward.APIkey;
            string url = $"https://empyrion-servers.com/api/?object=servers&element=votes&key={ServerKey}&format=JSON";
            string response = await Rest("GET", url);
            using (StreamWriter file = File.CreateText(ModPath + "VoteHistoryFromVoteSite.txt"))
            {
                Serializer serializer = new SerializerBuilder()
                    .Build();
                serializer.Serialize(file, response);
            }
            VoteHistoryJSON.LoadConfig();
            Dictionary<string, VoteHistoryJSON.VoteData> VoteDict = new Dictionary<string, VoteHistoryJSON.VoteData> { };
            foreach (VoteHistoryJSON.VoteData vote in VoteHistory.votes)
            {
                if (VoteDict.Keys.Contains(vote.steamid))
                {
                    VoteDict[vote.steamid].VoteCount = VoteDict[vote.steamid].VoteCount + 1;
                }
                else
                {
                    VoteHistoryJSON.VoteData newVote = vote;
                    newVote.VoteCount = 1;
                    VoteDict.Add(vote.steamid, newVote);
                }
            }
            foreach (string key in VoteDict.Keys)
            {
                CommonFunctions.LogFile("test.txt", key + " = " + VoteDict[key].VoteCount);
            }
            foreach (string id in VoteDict.Keys)
            {

            }
        }

        private async Task RestServerDetails()
        {
            string ServerKey = SetupYamlData.VoteReward.APIkey;
            string url = $"https://empyrion-servers.com/api/?object=servers&element=detail&key={ServerKey}";
            string response = await Rest("GET", url);
            using (StreamWriter file = File.CreateText(ModPath + "ServerDetailsFromVoteSite.txt"))
            {
                Serializer serializer = new SerializerBuilder()
                    .Build();
                serializer.Serialize(file, response);
            }
            ServerDetailsJSON.LoadConfig();
        }
        
        private async Task RestCurrentVoteCounts()
        {
            string url = $"https://empyrion-servers.com/api/?object=servers&element=votes&key={SetupYamlData.VoteReward.APIkey}&format=json";
            string response = await Rest("GET", url);
            using (StreamWriter file = File.CreateText(ModPath + "CurrentVotes.JSON"))
            {
                Serializer serializer = new SerializerBuilder()
                    .Build();
                serializer.Serialize(file, response);
            }
            string[] FixMe = File.ReadAllLines(ModPath + "CurrentVotes.JSON");
            List<string> FixMe2 = FixMe.ToList();
            FixMe2.RemoveAt(0);
            File.WriteAllLines(ModPath + "CurrentVotes.JSON",  FixMe2.ToArray());
            VoteHistoryJSON TheseVotes = JsonConvert.DeserializeObject<VoteHistoryJSON>(File.ReadAllText(ModPath + "CurrentVotes.JSON"), new Newtonsoft.Json.JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore
            });
            int[] TimestampArray = CommonFunctions.TimestampArray();
            File.WriteAllText(MyEmpyrionMod.ModPath + "Logs\\" + TimestampArray[0] + TimestampArray[1] + ".txt", "");
            foreach (VoteHistoryJSON.VoteData vote in TheseVotes.votes)
            {
                int EmpyrionID = 0;
                try
                {
                    EmpyrionID = SteamToEmpyrionID[vote.steamid];
                }
                catch { }
                VoteRewardYaml.Root PlayerRewards = new VoteRewardYaml.Root
                {
                    EmpyrionID = EmpyrionID,
                    Claimables = new List<SetupYaml.Claimable> { },
                    HealMe = 0,
                    Credits = 0,
                    ItemStacks = new List<VoteRewardYaml.ItemStacks> { },
                    RaffleTickets = 0
                };
                if (File.Exists(ModPath + "VoteRewards\\" + vote.steamid + ".yaml"))
                {
                    PlayerRewards = VoteRewardYaml.ReadYaml(ModPath + "VoteRewards\\" + vote.steamid + ".yaml");
                }

                if (vote.claimed == "0")
                {
                    SomethingClaimable.GiveReward(vote.steamid, PlayerRewards, SetupYamlData.VoteReward.DailyReward);
                    RestMarkClaimed(vote.steamid); //***
                }
                File.AppendAllText(MyEmpyrionMod.ModPath + "Logs\\" + TimestampArray[0] + TimestampArray[1] + ".txt", vote.steamid + "\r\n");

            }
        }
    }
}