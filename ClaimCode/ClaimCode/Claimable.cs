using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eleon.Modding;
using Eleon;

namespace ClaimCode
{
    class SomethingClaimable
    {
        internal static bool ClaimReward(int PlayerID, SetupYaml.Claimable Claim)
        {
            bool Failed = false;
            if (Claim.ClaimableType.ToLower() == "itempack")
            {
                //if Claimable type == ItemPack then request ItemExchange with Items using ItemsFileName
                try
                {
                    ItemStack[] ItemPackData = CommonFunctions.ReadItemStacks("ItemPacks\\", Claim.ItemPackFile);
                    Storage.StorableData StorableData = new Storage.StorableData
                    {
                        function = "ClaimItemPack",
                        Match = Convert.ToString(PlayerID),
                        Requested = "ItemExchange"
                    };
                    API.OpenItemExchange(PlayerID, "ClaimCode", "Please collect all your belongings.", "Close", ItemPackData, StorableData);

                    string SteamID = null;
                    try
                    {
                        //SteamID = DB.SteamID(PlayerID);
                        PlayerData? PlayerData = MyEmpyrionMod.modApi.Application.GetPlayerDataFor(PlayerID);
                        SteamID = PlayerData.Value.SteamId;
                    }
                    catch
                    {
                        Failed = true;
                        API.ServerTell(PlayerID, MyEmpyrionMod.ModShortName, "Unable to find database or database entry. Unable to log /claim.", true);
                    }
                    if (SteamID != null)
                    {
                        string newTimestamp = File.GetLastWriteTime(MyEmpyrionMod.ModPath + "KeysFiles\\" + Claim.KeysFile).ToString();
                        string FileVersion = File.GetLastWriteTime(MyEmpyrionMod.ModPath + "KeysFiles\\" + Claim.ItemPackFile).ToString();
                        CommonFunctions.LogFile("Logs\\" + MyEmpyrionMod.Timestamp + ".txt", newTimestamp + "  Type=" + Claim.ClaimableType + "  File=" + Claim.ItemPackFile + "  Player=" + SteamID + "  Fileversion=" + FileVersion);
                    }
                }
                catch
                {
                    Failed = true;
                    API.ServerTell(PlayerID, MyEmpyrionMod.ModShortName, "ItemPack does not exist.", true);
                }
            }
            else if (Claim.ClaimableType.ToLower() == "vessel" && Claim.Location.Type.ToLower() == "atplayer")
            {
                Storage.API2DialogBoxDelegateData DialogData = new Storage.API2DialogBoxDelegateData
                {
                    PlayerID = PlayerID
                };
                MyEmpyrionMod.NewEntityIDQueue.Add(PlayerID.ToString());
                Storage.StorableData StorableData = new Storage.StorableData
                {
                    function = "ClaimVesselAtPlayer",
                    Match = Convert.ToString(PlayerID),
                    Requested = "PlayerInfo",
                    API2DialogBoxData = DialogData
                };
                API.PlayerInfo(PlayerID, StorableData);
            }
            else if (Claim.ClaimableType.ToLower() == "vessel" && Claim.Location.Type.ToLower() == "staticlocation")
            {
                Storage.API2DialogBoxDelegateData DialogData = new Storage.API2DialogBoxDelegateData
                {
                    PlayerID = PlayerID
                };
                MyEmpyrionMod.NewEntityIDQueue.Add(PlayerID.ToString());
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
                Failed = true;
                API.ServerTell(PlayerID, MyEmpyrionMod.ModShortName, "Invalid Setup Detected, please contact Admin.", true);
            }
            return Failed;
        }

        internal static bool GiveReward(string SteamID, VoteRewardYaml.Root CurrentRewards, List<SetupYaml.Reward> RewardsList)
        {
            bool Failed = false;
            int VoteCount = 0;
            if (MyEmpyrionMod.VoteTracker.Keys.Contains(SteamID))
            {
                VoteCount = MyEmpyrionMod.VoteTracker[SteamID];
            }
            foreach (SetupYaml.Reward Reward in RewardsList)
            {
                if (Reward.Type == "ItemStacks")
                {
                    foreach (VoteRewardYaml.ItemStacks ListedItemStack in Reward.ItemStacks)
                    {
                        int Bonus = Convert.ToInt32(Math.Round(ListedItemStack.Quantity * ListedItemStack.Multiplier * VoteCount));
                        CommonFunctions.LogFile("Bonus.txt", SteamID + "  Bonus=" +  Bonus + "   Quantity=" + ListedItemStack.Quantity + "   Multiplier=" + ListedItemStack.Multiplier +  "   VoteCount =" + VoteCount);
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
            if (MyEmpyrionMod.VoteTracker.Keys.Contains(SteamID))
            {
                MyEmpyrionMod.VoteTracker[SteamID] = MyEmpyrionMod.VoteTracker[SteamID] + 1;
            }
            else
            {
                MyEmpyrionMod.VoteTracker.Add(SteamID, 1);
            }
            VoteRewardYaml.WriteYaml(MyEmpyrionMod.ModPath + "VoteRewards\\" + SteamID + ".yaml", CurrentRewards);
            return Failed;
        }

}
}
