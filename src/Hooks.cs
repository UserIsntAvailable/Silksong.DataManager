using HL = HarmonyLib;
using IO = System.IO;
using TC = TeamCherry;
using UE = UnityEngine;

namespace Silksong.DataManager;

[HL.HarmonyPatch(typeof(GameManager), nameof(GameManager.OnApplicationQuit))]
internal static class OnQuitGameHook
{
    private static void Postfix()
    {
        IO.Directory.CreateDirectory(DataPaths.ProfileDataDir);
        IO.Directory.CreateDirectory(DataPaths.GlobalDataDir);

        foreach (var mod in DataManagerPlugin.Instance.ManagedMods)
        {
            mod.SaveProfileData();
            mod.SaveGlobalData();
        }
    }
}

[HL.HarmonyPatch(
    typeof(GameManager),
    nameof(GameManager.SetLoadedGameData),
    typeof(SaveGameData),
    typeof(int)
)]
internal static class SaveDataLoadHook
{
    private static void Postfix(int saveSlot)
    {
        var mods = DataManagerPlugin.Instance.ManagedMods;

        if (saveSlot == 0)
        {
            foreach (var mod in mods)
            {
                if (mod.SaveData is { } saveData)
                {
                    saveData.UntypedSaveData = null;
                }
                if (mod.OnceSaveData is { } onceSaveData)
                {
                    onceSaveData.UntypedOnceSaveData = null;
                }
            }
            return;
        }

        foreach (var mod in mods)
        {
            mod.LoadSaveData(saveSlot);
            mod.LoadOnceSaveData(saveSlot);
        }

        DataManagerPlugin.Instance.SaveData ??= new();
    }
}

[HL.HarmonyPatch(
    typeof(GameManager),
    nameof(GameManager.SaveGame),
    [typeof(int), typeof(System.Action<bool>), typeof(bool), typeof(AutoSaveName)]
)]
internal static class SaveDataSaveHook
{
    private static void Prefix(int saveSlot, ref System.Action<bool> ogCallback)
    {
        if (saveSlot == 0)
        {
            return;
        }

        var ogCallbackCopy = ogCallback;
        ogCallback = (didSave) =>
        {
            ogCallbackCopy?.Invoke(didSave);

            if (!didSave)
            {
                return;
            }

            IO.Directory.CreateDirectory(DataPaths.SaveDataDir(saveSlot));

            foreach (var mod in DataManagerPlugin.Instance.ManagedMods)
            {
                mod.SaveSaveData(saveSlot);

                if (mod.SaveData is { } saveData)
                {
                    var optionalMods = DataManagerPlugin.Instance.SaveData?.OptionalMods;

                    if (saveData.UntypedIsOptional)
                    {
                        optionalMods?.Add(mod.Guid);
                    }
                    else
                    {
                        optionalMods?.Remove(mod.Guid);
                    }
                }
            }

            // TODO(UserIsntAvailable): Handle "Restore_Points"
        };
    }
}

[HL.HarmonyPatch(typeof(GameManager), nameof(GameManager.StartNewGame))]
internal static class OnceSetupHook
{
    private static void Postfix(GameManager __instance)
    {
        var saveSlot = __instance.profileID;
        if (saveSlot == 0)
        {
            return;
        }

        var mods = DataManagerPlugin.Instance.ManagedMods;
        var saveDir = DataPaths.OnceSaveDataDir(saveSlot);
        // Clear any existing modded data for this slot.
        // This can happen if a savefile is started with a mod active, and then the
        // game is closed before first saving that savefile.
        // Without clearing that data, it would be erroneously applied to the new savefile,
        // potentially even rendering it (spuriously) incompatible if said mod was uninstalled
        // in the meantime.
        DataManagerPlugin.ClearModdedSaveData(saveSlot);
        IO.Directory.CreateDirectory(saveDir);

        foreach (var mod in mods)
        {
            mod.SaveOnceSaveData(saveSlot);
        }
    }
}

[HL.HarmonyPatch(typeof(GameManager), nameof(GameManager.ClearSaveFile))]
internal static class SaveDataClearHook
{
    private static void Postfix(int saveSlot)
    {
        if (saveSlot != 0)
        {
            DataManagerPlugin.ClearModdedSaveData(saveSlot);
        }
    }
}

[HL.HarmonyPatch(typeof(UE.UI.SaveSlotButton), nameof(UE.UI.SaveSlotButton.ProcessSaveStats))]
internal static class ValidationHook
{
    private static TC.Localization.LocalisedString? vanillaSaveIncompatibleText;

    private static bool Prefix(
        UE.UI.SaveSlotButton __instance,
        ref bool __result,
        bool doAnimate,
        string errorInfo
    )
    {
        var saveSlot = __instance.SaveSlotIndex;
        var missingMods = DataManagerPlugin.Instance.MissingMods(saveSlot);
        if (missingMods.Count == 0)
        {
            if (vanillaSaveIncompatibleText is { } text)
            {
                DataManagerPlugin.SetSaveIncompatibleText(__instance, text);
                // Since the vanilla text is restored, we don't need to do this again.
                vanillaSaveIncompatibleText = null;
            }
            return true;
        }
        DataManagerPlugin.InstanceLogger.LogInfo(
            $"save slot {saveSlot} has save data for missing mods:"
        );
        foreach (var m in missingMods)
        {
            DataManagerPlugin.InstanceLogger.LogInfo(m);
        }
        vanillaSaveIncompatibleText = DataManagerPlugin.SetSaveIncompatibleText(
            __instance,
            new($"Mods.{DataManagerPlugin.Id}", "REQUIRED_MODS_MISSING")
        );
        // to match the behavior of the original method
        CheatManager.LastErrorText = errorInfo;
        __instance.ChangeSaveFileState(UE.UI.SaveSlotButton.SaveFileStates.Incompatible, doAnimate);
        __result = true;
        return false;
    }
}
