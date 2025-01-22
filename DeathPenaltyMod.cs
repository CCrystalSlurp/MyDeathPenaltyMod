using System.Reflection;
using HarmonyLib;
using RedLoader;
using SonsSdk;
using Sons.Events;
using Sons.Inventory;
using TheForest.Player.Actions;
using TheForest.Utils;

namespace DeathPenaltyMod;

public class DeathPenaltyMod : SonsMod
{
    private static int[] _itemsToBeRemoved = new int[]
    {
        ItemTools.Identifiers.I9mmAmmo,
        ItemTools.Identifiers.BoneArmour,
        ItemTools.Identifiers.BuckshotAmmo,
        ItemTools.Identifiers.C4Brick,
        ItemTools.Identifiers.CraftedArrow,
        ItemTools.Identifiers.CraftedBow,
        ItemTools.Identifiers.CraftedClub,
        ItemTools.Identifiers.CraftedSpear,
        ItemTools.Identifiers.CreepyArmour,
        ItemTools.Identifiers.CreepySkin,
        ItemTools.Identifiers.CrossbowAmmoBolt,
        ItemTools.Identifiers.DeerHide,
        ItemTools.Identifiers.DeerHideArmour,
        ItemTools.Identifiers.DeerHideArmour,
        ItemTools.Identifiers.Grenade,
        ItemTools.Identifiers.GrenadeAmmo,
        ItemTools.Identifiers.LeafArmour,
        ItemTools.Identifiers.LootPouch,
        ItemTools.Identifiers.Molotov,
        ItemTools.Identifiers.MolotovAmmo,
        ItemTools.Identifiers.PistolAmmoBox,
        ItemTools.Identifiers.RifleAmmo,
        ItemTools.Identifiers.ShotgunAmmoBoxBuckshot,
        ItemTools.Identifiers.ShotgunAmmoBoxSlug,
        ItemTools.Identifiers.SlugAmmo,
        ItemTools.Identifiers.SlugAmmo,
        ItemTools.Identifiers.StunGunAmmo,
        ItemTools.Identifiers.StunGunAmmoBox,
        ItemTools.Identifiers.TacticalBowAmmo,
        ItemTools.Identifiers.TimeBomb,
    };

    public DeathPenaltyMod()
    {
        // Uncomment any of these if you need a method to run on a specific update loop.
        //OnUpdateCallback = MyUpdateMethod;
        //OnLateUpdateCallback = MyLateUpdateMethod;
        //OnFixedUpdateCallback = MyFixedUpdateMethod;
        //OnGUICallback = MyGUIMethod;

        // Uncomment this to automatically apply harmony patches in your assembly.
        HarmonyPatchAll = true;
    }

    public static void FillDroppedInventoryPatch(
        ref Il2CppSystem.Collections.Generic.IReadOnlyDictionary<int, ItemInstanceManager.Items> itemsMap)
    {
        var overwrittenMap = new Il2CppSystem.Collections.Generic.Dictionary<int, ItemInstanceManager.Items>();

        var enumerator = LocalPlayer.Inventory._itemInstanceManager._itemsMap.GetEnumerator();

        while (enumerator.MoveNext())
        {
            Il2CppSystem.Collections.Generic.KeyValuePair<int, ItemInstanceManager.Items> current = enumerator._current;

            if (!_itemsToBeRemoved.Contains(current.Key))
            {
                overwrittenMap.Add(current.Key, current.Value);
            }
        }

        itemsMap =
            new Il2CppSystem.Collections.Generic.IReadOnlyDictionary<int, ItemInstanceManager.Items>(overwrittenMap
                .Pointer);

        RLog.Msg("Dropped Inventory Items overwritten");
    }

    private static void OnPlayerDied(object o)
    {
        LocalPlayer.Vitals.SetStrength(0);
        LocalPlayer.Vitals.SetStrengthLevel((LocalPlayer.Vitals._currentStrengthLevel - 1).ToString());
        LocalPlayer.Vitals.SetFullness(25);
        LocalPlayer.Vitals.SetHydration(25);
        LocalPlayer.Vitals.SetRest(25);
        RLog.Msg("Player died. Vitals Reset.");
    }

    protected override void OnInitializeMod()
    {
        // Do your early mod initialization which doesn't involve game or sdk references here
        Config.Init();
    }

    protected override void OnSdkInitialized()
    {
        // Do your mod initialization which involves game or sdk references here
        // This is for stuff like UI creation, event registration etc.
        DeathPenaltyModUi.Create();

        EventRegistry.Register(GameEvent.LocalPlayerDied, (EventRegistry.SubscriberCallback)OnPlayerDied);

        // Add in-game settings ui for your mod.
        // SettingsRegistry.CreateSettings(this, null, typeof(Config));
    }

    protected override void OnGameStart()
    {
        // This is called once the player spawns in the world and gains control.
        var original =
            typeof(PlayerRetrieveDroppedInventoryAction).GetMethod(nameof(PlayerRetrieveDroppedInventoryAction
                .AddInventoryItems));
        var prefix = typeof(DeathPenaltyMod).GetMethod(nameof(FillDroppedInventoryPatch));

        PatchMethod(original, prefix);

        RLog.Msg("Mod initialized");
    }

    private void PatchMethod(MethodInfo original, MethodInfo prefix)
    {
        if (original == null)
        {
            RLog.Msg("Could not patch method: original is null");
            return;
        }

        if (prefix == null)
        {
            RLog.Msg("Could not patch method: prefix is null");
            return;
        }

        try
        {
            HarmonyInstance.Patch(original, prefix: new HarmonyMethod(prefix));
        }
        catch (Exception e)
        {
            RLog.Msg("Could not patch method: " + e.Message + "\n" + e.StackTrace);
        }
    }
}