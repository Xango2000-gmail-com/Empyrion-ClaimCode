using System;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.Data.SQLite;
//using System.Data.Common;
using Eleon.Modding;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net;


namespace ClaimCode
{
    class DB
    {
        /*
        public class LevelFinder
        {
            public int Level { get; set; }
            public int GameTime { get; set; }
        }

        //static SQLiteConnection sqliteCon = new SQLiteConnection(@"Data Source=K:\\SteamLauncher\\steamapps\\common\\Empyrion - Dedicated Server\\Saves\\Games\\" + MyEmpyrionMod.SetupYamlData.SaveGameName + "\\global.db");
        static SQLiteConnection sqliteCon = new SQLiteConnection(@"Data Source=..\\Saves\\Games\\" + MyEmpyrionMod.SetupYamlData.General.SaveGameName + "\\global.db");

        internal class EntityData
        {
            public string Name;
            public int EntityID;
            public int PosX;
            public int PosY;
            public int PosZ;
            public int RotX;
            public int RotY;
            public int RotZ;
            public int pfid;
            public int facgroup;
            public int facid;
            public int etype;
            public int Health;
            public int isStructure;
            public int isProxy;
            public int isPoi;
            public int isRemoved;
            public int BelongsTo;
            public int DockedTo;
        }

        internal class PlayfieldData
        {
            public int pfid;
            public string Name;
            public string pfType;
            public int mapType;
            public int size;
            public int pvp;
        }

        internal class StructureData
        {
            public int EntityID;
            public int LastVisitedTicks;
            public int cntBlocks;
            public int cntDevices;
            public int cntTriangles;
            public int cntLights;
            public int ClassNr;
            public int isPowered;
            public int Fuel;
            public int CoreType;
            public int PilotId;
            public int HasTeleporter;
            public int HasSPUnlocked;
            public int HasSPLocked;
            public int PlayerCreated;
            public int FromBpLibrary;
            public int Discovery;
            public int StationInterface;
            public int SizeX;
            public int SizeY;
            public string BpName;
        }

        public static void Query(string CommandText)
        {
            //for DB.Read("SELECT entityid, name FROM Entities WHERE pfid = 8 and fid = 100 ");
            sqliteCon.Open();
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            Dictionary<string, string> QueryData = new Dictionary<string, string> { };
            while (dataReader.Read())
            {
                CommonFunctions.Log("EmpyrionID: " + dataReader.GetFieldValue<Int64>(0) + "  EntityName= " + dataReader.GetFieldValue<string>(1));
            }
            dataReader.Close();
            sqliteCon.Close();
        }

        public static EntityData LookupPlayer(int EmpyrionID)
        {
            sqliteCon.Open();
            string CommandText = "SELECT * FROM Entities WHERE entityid = " + EmpyrionID + " AND etype = 1";
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            EntityData QueryData = new EntityData() { };
            while (dataReader.Read())
            {
                QueryData.Name = Convert.ToString(dataReader.GetValue(2));
                QueryData.EntityID = Convert.ToInt32(dataReader.GetValue(0));
                QueryData.facid = Convert.ToInt32(dataReader.GetValue(5));
                QueryData.facgroup = Convert.ToInt32(dataReader.GetValue(4));
                QueryData.pfid = Convert.ToInt32(dataReader.GetValue(1));
                QueryData.PosX = Convert.ToInt32(dataReader.GetValue(6));
                QueryData.PosY = Convert.ToInt32(dataReader.GetValue(7));
                QueryData.PosZ = Convert.ToInt32(dataReader.GetValue(8));
                QueryData.RotX = Convert.ToInt32(dataReader.GetValue(9));
                QueryData.RotY = Convert.ToInt32(dataReader.GetValue(10));
                QueryData.RotZ = Convert.ToInt32(dataReader.GetValue(11));
                QueryData.Health = Convert.ToInt32(dataReader.GetValue(12));
            }
            dataReader.Close();
            sqliteCon.Close();
            return QueryData;
        }

        public static EntityData LookupEntity(int EntityID)
        {
            sqliteCon.Open();
            string CommandText = "SELECT * FROM Entities WHERE entityid = " + EntityID;
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            EntityData QueryData = new EntityData() { };
            while (dataReader.Read())
            {
                QueryData.EntityID = Convert.ToInt32(dataReader.GetValue(0));
                QueryData.pfid = Convert.ToInt32(dataReader.GetValue(1));
                QueryData.Name = Convert.ToString(dataReader.GetValue(2));
                QueryData.etype = Convert.ToInt32(dataReader.GetValue(3));
                QueryData.facgroup = Convert.ToInt32(dataReader.GetValue(4));
                QueryData.facid = Convert.ToInt32(dataReader.GetValue(5));
                QueryData.PosX = Convert.ToInt32(dataReader.GetValue(6));
                QueryData.PosY = Convert.ToInt32(dataReader.GetValue(7));
                QueryData.PosZ = Convert.ToInt32(dataReader.GetValue(8));
                QueryData.RotX = Convert.ToInt32(dataReader.GetValue(9));
                QueryData.RotY = Convert.ToInt32(dataReader.GetValue(10));
                QueryData.RotZ = Convert.ToInt32(dataReader.GetValue(11));
                QueryData.Health = Convert.ToInt32(dataReader.GetValue(12));
                QueryData.isStructure = Convert.ToInt32(dataReader.GetValue(13));
                QueryData.isRemoved = Convert.ToInt32(dataReader.GetValue(14));
                QueryData.isProxy = Convert.ToInt32(dataReader.GetValue(16));
                QueryData.isPoi = Convert.ToInt32(dataReader.GetValue(17));
                try
                {
                    QueryData.BelongsTo = Convert.ToInt32(dataReader.GetValue(18));

                }
                catch
                {
                    QueryData.BelongsTo = 0;
                }
                try
                {
                    QueryData.DockedTo = Convert.ToInt32(dataReader.GetValue(22));
                }
                catch
                {
                    QueryData.DockedTo = 0;
                }
            }
            dataReader.Close();
            sqliteCon.Close();
            return QueryData;
        }

        public static ItemStack[] StructureDeviceCounts(int EntityID)
        {
            List<ItemStack> DeviceCounts = new List<ItemStack> { };
            sqliteCon.Open();
            string CommandText = "SELECT * FROM Entities WHERE entityid = " + EntityID;
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            EntityData QueryData = new EntityData() { };
            while (dataReader.Read())
            {
                ItemStack DeviceCount = new ItemStack
                {
                    id = Convert.ToInt32(dataReader.GetFieldValue<Int64>(0)),
                    count = Convert.ToInt32(dataReader.GetFieldValue<Int64>(1)),
                    ammo = 0,
                    slotIdx = 0,
                    decay = 0
                };
                DeviceCounts.Add(DeviceCount);
                //dataReader.GetFieldValue<Int64>(0) + dataReader.GetFieldValue<Int64>(1));
            }
            ItemStack[] ItemStackArray = DeviceCounts.ToArray();
            return ItemStackArray;
        }

        public static List<int> Docked(int EntityID)
        {
            sqliteCon.Open();
            string CommandText = "SELECT entityid FROM Entities WHERE dockedto = " + EntityID;
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            List<int> QueryData = new List<int> { };
            while (dataReader.Read())
            {
                try
                {
                    QueryData.Add(Convert.ToInt32(dataReader.GetFieldValue<Int64>(0)));
                }
                catch { };
            }
            dataReader.Close();
            sqliteCon.Close();
            return QueryData;
        }

        public static List<EntityData> Motorcycle()
        {
            sqliteCon.Open();
            string CommandText = "SELECT * FROM Entities WHERE name = 'PlayerBike' AND isremoved = 0";
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            List<EntityData> ReturnData = new List<EntityData> { };
            while (dataReader.Read())
            {
                try
                {
                    EntityData QueryData = new EntityData { };
                    QueryData.EntityID = Convert.ToInt32(dataReader.GetValue(0));
                    QueryData.pfid = Convert.ToInt32(dataReader.GetValue(1));
                    QueryData.Name = Convert.ToString(dataReader.GetValue(2));
                    QueryData.etype = Convert.ToInt32(dataReader.GetValue(3));
                    QueryData.facgroup = Convert.ToInt32(dataReader.GetValue(4));
                    QueryData.facid = Convert.ToInt32(dataReader.GetValue(5));
                    QueryData.PosX = Convert.ToInt32(dataReader.GetValue(6));
                    QueryData.PosY = Convert.ToInt32(dataReader.GetValue(7));
                    QueryData.PosZ = Convert.ToInt32(dataReader.GetValue(8));
                    QueryData.RotX = Convert.ToInt32(dataReader.GetValue(9));
                    QueryData.RotY = Convert.ToInt32(dataReader.GetValue(10));
                    QueryData.RotZ = Convert.ToInt32(dataReader.GetValue(11));
                    QueryData.Health = Convert.ToInt32(dataReader.GetValue(12));
                    QueryData.isStructure = Convert.ToInt32(dataReader.GetValue(13));
                    QueryData.isRemoved = Convert.ToInt32(dataReader.GetValue(14));
                    QueryData.isProxy = Convert.ToInt32(dataReader.GetValue(16));
                    QueryData.isPoi = Convert.ToInt32(dataReader.GetValue(17));
                    QueryData.BelongsTo = Convert.ToInt32(dataReader.GetValue(18));
                    ReturnData.Add(QueryData);
                }
                catch { };
            }
            dataReader.Close();
            sqliteCon.Close();
            return ReturnData;
        }

        public static PlayfieldData LookupPlayfield(int pfID)
        {
            sqliteCon.Open();
            string CommandText = "SELECT * FROM Playfields WHERE pfid = " + pfID;
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            PlayfieldData ReturnData = new PlayfieldData { };
            while (dataReader.Read())
            {
                try
                {
                    ReturnData.pfid = Convert.ToInt32(dataReader.GetValue(1));
                    ReturnData.pfType = Convert.ToString(dataReader.GetValue(3));
                    ReturnData.Name = Convert.ToString(dataReader.GetValue(4));
                    ReturnData.size = Convert.ToInt32(dataReader.GetValue(5));
                    ReturnData.mapType = Convert.ToInt32(dataReader.GetValue(6));
                    ReturnData.pvp = Convert.ToInt32(dataReader.GetValue(9));
                }
                catch { };
            }
            dataReader.Close();
            sqliteCon.Close();
            return ReturnData;
        }

        public static int LookupPlayerLevel(int PlayerID)
        {
            int level = 0;
            int gameTime = 0;
            string CommandText = "SELECT level, gametime FROM PlayerLevelUp WHERE entityid = " + PlayerID;
            sqliteCon.Open();
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            List<LevelFinder> LevelsList = new List<LevelFinder> { };
            while (dataReader.Read())
            {
                LevelFinder LF = new LevelFinder
                {
                    Level = Convert.ToInt32(dataReader.GetValue(0)),
                    GameTime = Convert.ToInt32(dataReader.GetValue(1))
                };
                LevelsList.Add(LF);
            }
            dataReader.Close();
            sqliteCon.Close();
            foreach (LevelFinder LFData in LevelsList)
            {
                if (LFData.GameTime > gameTime)
                {
                    gameTime = LFData.GameTime;
                    level = LFData.Level;
                }
            }
            return level;
        }

        public static int EmpyrionID(string SteamID)
        {
            int EmpyrionID = 0;
            string CommandText = "SELECT entityid FROM LoginLogoff WHERE playerid = " + SteamID;
            sqliteCon.Open();
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            while (dataReader.Read())
            {
                EmpyrionID = Convert.ToInt32(dataReader.GetValue(0));
                break;
            }
            dataReader.Close();
            sqliteCon.Close();
            return EmpyrionID;
        }

        public static StructureData Structures(int EntityID)
        {
            StructureData Data = new StructureData { };
            string CommandText = "SELECT * FROM Structures WHERE entityid = " + EntityID;
            sqliteCon.Open();
            SQLiteCommand selectCommand = new SQLiteCommand(CommandText, sqliteCon);
            SQLiteDataReader dataReader = selectCommand.ExecuteReader();
            while (dataReader.Read())
            {
                Data.EntityID = Convert.ToInt32(dataReader.GetValue(0));
                Data.LastVisitedTicks = Convert.ToInt32(dataReader.GetValue(1));
                Data.cntBlocks = Convert.ToInt32(dataReader.GetValue(2));
                Data.cntDevices = Convert.ToInt32(dataReader.GetValue(3));
                Data.cntTriangles = Convert.ToInt32(dataReader.GetValue(4));
                Data.cntLights = Convert.ToInt32(dataReader.GetValue(5));
                Data.ClassNr = Convert.ToInt32(dataReader.GetValue(6));
                Data.isPowered = Convert.ToInt32(dataReader.GetValue(7));
                Data.Fuel = Convert.ToInt32(dataReader.GetValue(8));
                Data.CoreType = Convert.ToInt32(dataReader.GetValue(9));
                try
                {
                    if (dataReader.GetValue(10) != null)
                    {
                        Data.PilotId = Convert.ToInt32(dataReader.GetValue(10));
                    }
                }
                catch
                {
                    Data.PilotId = 0;
                }
                Data.HasTeleporter = Convert.ToInt32(dataReader.GetValue(11));
                Data.HasSPUnlocked = Convert.ToInt32(dataReader.GetValue(12));
                Data.HasSPLocked = Convert.ToInt32(dataReader.GetValue(13));
                Data.PlayerCreated = Convert.ToInt32(dataReader.GetValue(14));
                Data.FromBpLibrary = Convert.ToInt32(dataReader.GetValue(15));
                Data.Discovery = Convert.ToInt32(dataReader.GetValue(16));
                Data.StationInterface = Convert.ToInt32(dataReader.GetValue(17));
                Data.SizeX = Convert.ToInt32(dataReader.GetValue(18));
                Data.SizeY = Convert.ToInt32(dataReader.GetValue(19));
                Data.BpName = Convert.ToString(dataReader.GetValue(20));
            }
            dataReader.Close();
            sqliteCon.Close();
            return Data;
        }
*/
    }
}
