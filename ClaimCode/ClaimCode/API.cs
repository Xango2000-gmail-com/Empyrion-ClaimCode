using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eleon.Modding;
using Eleon;

namespace ClaimCode
{
    class API
    {
        public static void Alert(int Target, string Message, string Alert, float Time)
        {
            byte prio = 2;
            if (Alert == "red")
            {
                prio = 0;
            }
            else if (Alert == "yellow")
            {
                prio = 1;
            }
            if (Target == 0)
            {
                Storage.GameAPI.Game_Request(CmdId.Request_InGameMessage_AllPlayers, (ushort)Storage.CurrentSeqNr, new IdMsgPrio(Target, Message, prio, Time));
            }
            else if (Target < 999)
            {
                Storage.GameAPI.Game_Request(CmdId.Request_InGameMessage_Faction, (ushort)Storage.CurrentSeqNr, new IdMsgPrio(Target, Message, prio, Time));
            }
            else if (Target > 999)
            {
                Storage.GameAPI.Game_Request(CmdId.Request_InGameMessage_SinglePlayer, (ushort)Storage.CurrentSeqNr, new IdMsgPrio(Target, Message, prio, Time));
            }
        }

        public static void Chat(string Type, int Target, string Message)
        {
            if (Type == "Global")
            {
                API.ConsoleCommand("say '" + Message + "'");
            }
            else if (Type == "Faction")
            {
                API.ConsoleCommand("say f:" + Target + " '" + Message + "'");
            }
            else if (Type == "Player")
            {
                API.ConsoleCommand("say p:" + Target + " '" + Message + "'");
            }
            Alert(Target, Message, "Blue", 3);
        }

        public static void ServerSay(string SayAs, string Message, bool TakeFocus)
        {
            MessageData SendableMsgData = new MessageData
            {
                Channel = Eleon.MsgChannel.Global,
                Text = Message,
                SenderNameOverride = SayAs
            };
            if (TakeFocus)
            {
                SendableMsgData = new MessageData
                {
                    Channel = Eleon.MsgChannel.Global,
                    Text = Message,
                    SenderNameOverride = SayAs,
                    SenderType = Eleon.SenderType.ServerPrio
                };
            }
            MyEmpyrionMod.modApi.Application.SendChatMessage(SendableMsgData);
        }

        public static void ServerTell(int TargetID, string TellAs, string Message, bool TakeFocus)
        {
            MessageData SendableMsgData = new MessageData
            {
                Channel = Eleon.MsgChannel.SinglePlayer,
                RecipientEntityId = TargetID,
                Text = Message,
                SenderNameOverride = TellAs
            };
            if (TakeFocus)
            {
                SendableMsgData = new MessageData
                {
                    Channel = Eleon.MsgChannel.SinglePlayer,
                    RecipientEntityId = TargetID,
                    Text = Message,
                    SenderNameOverride = TellAs,
                    SenderType = Eleon.SenderType.ServerPrio
                };
            }
            MyEmpyrionMod.modApi.Application.SendChatMessage(SendableMsgData);
        }

        public static void PlayerInfo(int playerID, Storage.StorableData StoreableData)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            MyEmpyrionMod.SeqNrStorage[Storage.CurrentSeqNr] = StoreableData;
            Storage.GameAPI.Game_Request(CmdId.Request_Player_Info, (ushort)Storage.CurrentSeqNr, new Id(playerID));
        }

        public static void TextWindowOpen(int TargetPlayer, string Message, String ConfirmText, String CancelText, Storage.StorableData StoreableData)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            MyEmpyrionMod.SeqNrStorage[Storage.CurrentSeqNr] = StoreableData;
            Storage.GameAPI.Game_Request(CmdId.Request_ShowDialog_SinglePlayer, (ushort)Storage.CurrentSeqNr, new DialogBoxData
            {
                Id = Convert.ToInt32(TargetPlayer),
                MsgText = Message,
                NegButtonText = CancelText,
                PosButtonText = ConfirmText
            });
        }

        public static void Gents(string playfield, Storage.StorableData StoreableData)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            MyEmpyrionMod.SeqNrStorage[Storage.CurrentSeqNr] = StoreableData;
            Storage.GameAPI.Game_Request(CmdId.Request_GlobalStructure_Update, (ushort)Storage.CurrentSeqNr, new PString(playfield));
        }

        public static void ConsoleCommand(String Sendable)
        {
            Storage.GameAPI.Game_Request(CmdId.Request_ConsoleCommand, (ushort)Storage.CurrentSeqNr, new PString(Sendable));
        }

        public static void OpenItemExchange(int PlayerID, string Title, string Message, string ButtonText, ItemStack[] Inventory, Storage.StorableData StoreableData)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            MyEmpyrionMod.SeqNrStorage[Storage.CurrentSeqNr] = StoreableData;
            Storage.GameAPI.Game_Request(CmdId.Request_Player_ItemExchange, (ushort)Storage.CurrentSeqNr, new ItemExchangeInfo(PlayerID, Title, Message, ButtonText, Inventory));
        }

        public static void Credits(int PlayerID, Double CreditChange)
        {
            Storage.GameAPI.Game_Request(CmdId.Request_Player_AddCredits, (ushort)Storage.CurrentSeqNr, new IdCredits(PlayerID, CreditChange));
        }

        public static void BlockData(int EntityID, Storage.StorableData StoreableData)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            MyEmpyrionMod.SeqNrStorage[Storage.CurrentSeqNr] = StoreableData;
            Storage.GameAPI.Game_Request(CmdId.Request_Structure_BlockStatistics, (ushort)Storage.CurrentSeqNr, new Id(EntityID));
        }

        public static void Delete(int EntityID)
        {
            try
            {
                Storage.GameAPI.Game_Request(CmdId.Request_Entity_Destroy, (ushort)Storage.CurrentSeqNr, new Id(EntityID));
            }
            catch { }
        }

        public static void Delete(int EntityID, string Playfield)
        {
            try
            {
                Storage.GameAPI.Game_Request(CmdId.Request_Entity_Destroy2, (ushort)Storage.CurrentSeqNr, new IdPlayfield(EntityID, Playfield));
            }
            catch { }
        }

        public static void CreateEntityID(Storage.StorableData StoreableData)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            MyEmpyrionMod.SeqNrStorage[Storage.CurrentSeqNr] = StoreableData;
            Storage.GameAPI.Game_Request(CmdId.Request_NewEntityId, (ushort)Storage.CurrentSeqNr, null);
        }

        public static void CreateEntity(int EntityID, string Playfield, PVector3 Pos, PVector3 Rot, string Name, byte EntityType, string TypeName, string prefabname, string prefabDir, byte FactionGroup, int FactionID, string ExportedEntityDat)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            EntitySpawnInfo EntityParameters = new EntitySpawnInfo
            {
                forceEntityId = EntityID,
                playfield = Playfield,
                pos = Pos,
                rot = Rot,
                name = Name,
                type = EntityType,
                entityTypeName = TypeName,
                prefabName = prefabname,
                factionGroup = FactionGroup,
                factionId = FactionID
            };
            if (prefabDir != null)
            {
                EntityParameters.prefabDir = prefabDir;
            }
            if (ExportedEntityDat != null)
            {
                EntityParameters.exportedEntityDat = ExportedEntityDat;
            }
            Storage.GameAPI.Game_Request(CmdId.Request_Entity_Spawn, (ushort)Storage.CurrentSeqNr, EntityParameters);
            //return Storage.CurrentSeqNr;
        }

        public static void CreatePrefabForPlayer(int EntityID, string Playfield, PVector3 Pos, PVector3 Rot, string Name, string entityType, string prefabname, int FactionID)
        {
            byte EntityType = 0;
            if (entityType.ToLower() == "base")
            {
                EntityType = 2;
            }
            else if (entityType.ToLower() == "cv")
            {
                EntityType = 3;
            }
            else if (entityType.ToLower() == "sv")
            {
                EntityType = 4;
            }
            else if (entityType.ToLower() == "hv")
            {
                EntityType = 5;
            }
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            EntitySpawnInfo EntityParameters = new EntitySpawnInfo
            {
                forceEntityId = EntityID,
                playfield = Playfield,
                pos = Pos,
                rot = Rot,
                //name = Name,
                type = EntityType,
                prefabName = prefabname,
                factionGroup = 1,
                factionId = FactionID
            };
            if (Name != null) EntityParameters.name = Name;
            Storage.GameAPI.Game_Request(CmdId.Request_Entity_Spawn, (ushort)Storage.CurrentSeqNr, EntityParameters);
        }

        public static void GiveItem(int playerID, ItemStack Item, Storage.StorableData StoreableData)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            MyEmpyrionMod.SeqNrStorage[Storage.CurrentSeqNr] = StoreableData;
            IdItemStack Sendable = new IdItemStack()
            {
                id = playerID,
                itemStack = Item
            };
            Storage.GameAPI.Game_Request(CmdId.Request_Player_AddItem, (ushort)Storage.CurrentSeqNr, Sendable);
        }

        public static void Mark(int ClientID, string MarkName, int x, int y, int z, string Type)
        {
            string sendable = "remoteex cl=" + ClientID + " \'marker add name=" + MarkName + " pos=" + x + "," + y + "," + z + " " + Type + "\'";
            CommonFunctions.Debug(sendable);
            ConsoleCommand(sendable);
        }

        public static void SetLevel(int ClientID, int Level, Storage.StorableData StorableData)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            MyEmpyrionMod.SeqNrStorage[Storage.CurrentSeqNr] = StorableData;
            string Sendable = "remoteex cl=" + ClientID + " \'level = " + Level + "\'";
            Storage.GameAPI.Game_Request(CmdId.Request_ConsoleCommand, (ushort)Storage.CurrentSeqNr, new PString(Sendable));
            if (Level == 25)
            {
                Sendable = "remoteex cl=" + ClientID + " \'level x+ 1\'";
                Storage.GameAPI.Game_Request(CmdId.Request_ConsoleCommand, (ushort)Storage.CurrentSeqNr, new PString(Sendable));

            }
        }

        public static void TeleportPlayer(int PlayerID, string Playfield, int Posx, int Posy, int Posz, int Rotx, int Roty, int Rotz)
        {
            PVector3 pos = new PVector3
            {
                x = Posx,
                y = Posy,
                z = Posz
            };
            PVector3 rot = new PVector3
            {
                x = Rotx,
                y = Roty,
                z = Rotz
            };
            Storage.GameAPI.Game_Request(CmdId.Request_Player_ChangePlayerfield, (ushort)Storage.CurrentSeqNr, new IdPlayfieldPositionRotation(PlayerID, Playfield, pos, rot));
        }

        public static void TeleportPlayer(int PlayerID, string Playfield, int Posx, int Posy, int Posz)
        {
            PVector3 pos = new PVector3
            {
                x = Posx,
                y = Posy,
                z = Posz
            };
            PVector3 rot = new PVector3
            {
                x = 0,
                y = 0,
                z = 0
            };

            Storage.GameAPI.Game_Request(CmdId.Request_Player_ChangePlayerfield, (ushort)Storage.CurrentSeqNr, new IdPlayfieldPositionRotation(PlayerID, Playfield, pos, rot));
        }

        public static void TeleportPlayer(int PlayerID, string Playfield, PVector3 pos, PVector3 rot)
        {
            Storage.GameAPI.Game_Request(CmdId.Request_Player_ChangePlayerfield, (ushort)Storage.CurrentSeqNr, new IdPlayfieldPositionRotation(PlayerID, Playfield, pos, rot));
        }

        public static void TeleportPlayer(int PlayerID, string Playfield, PVector3 pos)
        {
            PVector3 rot = new PVector3
            {
                x = 0,
                y = 0,
                z = 0
            };
            Storage.GameAPI.Game_Request(CmdId.Request_Player_ChangePlayerfield, (ushort)Storage.CurrentSeqNr, new IdPlayfieldPositionRotation(PlayerID, Playfield, pos, rot));
        }

        public static void TeleportEntity(int EntityID, int Posx, int Posy, int Posz)
        {
            PVector3 pos = new PVector3
            {
                x = Posx,
                y = Posy,
                z = Posz
            };
            PVector3 rot = new PVector3
            {
                x = 0,
                y = 0,
                z = 0
            };
            Storage.GameAPI.Game_Request(CmdId.Request_Entity_Teleport, (ushort)Storage.CurrentSeqNr, new IdPositionRotation(EntityID, pos, rot));
        }

        public static void TeleportEntity(int EntityID, int Posx, int Posy, int Posz, int Rotx, int Roty, int Rotz)
        {
            PVector3 pos = new PVector3
            {
                x = Posx,
                y = Posy,
                z = Posz
            };
            PVector3 rot = new PVector3
            {
                x = Rotx,
                y = Roty,
                z = Rotz
            };
            Storage.GameAPI.Game_Request(CmdId.Request_Entity_Teleport, (ushort)Storage.CurrentSeqNr, new IdPositionRotation(EntityID, pos, rot));
        }

        public static void TeleportEntity(int EntityID, PVector3 pos)
        {
            PVector3 rot = new PVector3
            {
                x = 0,
                y = 0,
                z = 0
            };
            Storage.GameAPI.Game_Request(CmdId.Request_Entity_Teleport, (ushort)Storage.CurrentSeqNr, new IdPositionRotation(EntityID, pos, rot));
        }

        public static void TeleportEntity(int EntityID, PVector3 pos, PVector3 rot)
        {
            Storage.GameAPI.Game_Request(CmdId.Request_Entity_Teleport, (ushort)Storage.CurrentSeqNr, new IdPositionRotation(EntityID, pos, rot));
        }

        public static void TeleportEntity(int EntityID, string Playfield, int Posx, int Posy, int Posz, int Rotx, int Roty, int Rotz)
        {
            PVector3 pos = new PVector3
            {
                x = Posx,
                y = Posy,
                z = Posz
            };
            PVector3 rot = new PVector3
            {
                x = Rotx,
                y = Roty,
                z = Rotz
            };
            Storage.GameAPI.Game_Request(CmdId.Request_Entity_ChangePlayfield, (ushort)Storage.CurrentSeqNr, new IdPlayfieldPositionRotation(EntityID, Playfield, pos, rot));
        }

        public static void LoadPlayfield(string Playfield, float nSecs, int nProcessId, Storage.StorableData StorableData)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            MyEmpyrionMod.SeqNrStorage[Storage.CurrentSeqNr] = StorableData;
            Storage.GameAPI.Game_Request(CmdId.Request_Load_Playfield, (ushort)Storage.CurrentSeqNr, new PlayfieldLoad(nSecs, Playfield, nProcessId));
        }

        public static void PlayfieldsList(Storage.StorableData StorableData)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            MyEmpyrionMod.SeqNrStorage[Storage.CurrentSeqNr] = StorableData;
            Storage.GameAPI.Game_Request(CmdId.Request_Playfield_List, (ushort)Storage.CurrentSeqNr, null);
        }

        public static void PlayerInfoChange(PlayerInfoSet PlayerInfo)
        {
            Storage.GameAPI.Game_Request(CmdId.Request_Player_SetPlayerInfo, (ushort)Storage.CurrentSeqNr, PlayerInfo);
        }

        public static void PlayerInventorySet(int PlayerID, ItemStack[] Backpack, ItemStack[] Toolbar)
        {
            ItemStack[] blankIS = new ItemStack[] { };
            Inventory InvData = new Inventory()
            {
                playerId = PlayerID
            };
            if (Backpack != blankIS)
            {
                InvData.bag = Backpack;
            }
            if (Toolbar != blankIS)
            {
                InvData.toolbelt = Toolbar;
            }
            Storage.GameAPI.Game_Request(CmdId.Request_Player_SetInventory, (ushort)Storage.CurrentSeqNr, InvData);
        }

        public static void Factory(BlueprintResources bpResources)
        {
            Storage.GameAPI.Game_Request(CmdId.Request_Blueprint_Resources, (ushort)Storage.CurrentSeqNr, bpResources);
        }

        public static void InventoryGet(int PlayerID, Storage.StorableData StorableData)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            MyEmpyrionMod.SeqNrStorage[Storage.CurrentSeqNr] = StorableData;
            Storage.GameAPI.Game_Request(CmdId.Request_Player_GetInventory, (ushort)Storage.CurrentSeqNr, new Id(PlayerID));
        }

        public static void InventorySet(int PlayerID, ItemStack[] Toolbelt, ItemStack[] Backpack, Storage.StorableData StorableData)
        {
            Storage.CurrentSeqNr = CommonFunctions.SeqNrGenerator(Storage.CurrentSeqNr);
            MyEmpyrionMod.SeqNrStorage[Storage.CurrentSeqNr] = StorableData;
            Storage.GameAPI.Game_Request(CmdId.Request_Player_SetInventory, (ushort)Storage.CurrentSeqNr, new Inventory(PlayerID, Toolbelt, Backpack));
        }

        public static void Regenerate(int EntityID, int PlayfieldProcessID)
        {

            string Sendable = "remoteex pf=" + PlayfieldProcessID + " 'regenerate " + EntityID +"'";
            ConsoleCommand(Sendable);
        }

        
    }
}
