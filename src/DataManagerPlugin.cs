using static System.Linq.Enumerable;
using Bep = BepInEx;
using CG = System.Collections.Generic;
using HL = HarmonyLib;
using IO = System.IO;
using TC = TeamCherry;
using UE = UnityEngine;

namespace Silksong.DataManager;

[Bep.BepInAutoPlugin(id: "org.silksong-modding.datamanager")]
[Bep.BepInDependency("org.silksong-modding.i18n")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public partial class DataManagerPlugin : Bep.BaseUnityPlugin, ISaveDataMod<DataManagerSaveData>
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
{
    private ManagedMod _managedInstance = null!;

    // These properties will never be accessed before Start executes.
    internal static DataManagerPlugin Instance { get; private set; } = null!;
    internal static Bep.Logging.ManualLogSource InstanceLogger => Instance.Logger;

    internal CG.List<ManagedMod> ManagedMods = [];

    private void Awake()
    {
        Instance = this;
        new HL.Harmony(Id).PatchAll();

        Logger.LogInfo("Mod Loaded");
    }

    // We must use Start instead of Awake here - PluginInfos does not contain
    // any mod instances when Awake runs.
    private void Start()
    {
        _ = ManagedMod.TryCreate(this, out var managedMod);
        _managedInstance = managedMod!;

        foreach (var (guid, info) in Bep.Bootstrap.Chainloader.PluginInfos)
        {
            if (
                info?.Instance is not { } plugin
                || guid == Id
                || !ManagedMod.TryCreate(plugin, out managedMod)
            )
                continue;

            managedMod.LoadProfileData();
            managedMod.LoadGlobalData();
            ManagedMods.Add(managedMod);
        }

        ManagedMods.Add(_managedInstance);
    }

    internal static void ClearModdedSaveData(int saveSlot)
    {
        var saveDir = DataPaths.SaveDataDir(saveSlot);
        try
        {
            IO.Directory.Delete(saveDir, true);
            DataManagerPlugin.InstanceLogger.LogInfo(
                $"Cleared modded save data for slot {saveSlot}"
            );
        }
        catch (IO.DirectoryNotFoundException)
        {
            DataManagerPlugin.InstanceLogger.LogInfo(
                $"No modded save data to clear for slot {saveSlot}"
            );
        }
        catch (System.Exception err)
        {
            DataManagerPlugin.InstanceLogger.LogError(
                $"Error clearing modded save data for slot {saveSlot}: {err}"
            );
        }
    }

    internal static TC.Localization.LocalisedString SetSaveIncompatibleText(
        UE.UI.SaveSlotButton button,
        TC.Localization.LocalisedString s
    )
    {
        var smallDescLocalizer = button
            .saveIncompatibleText.gameObject.transform.Find("Small Desc")
            .gameObject.GetComponent<AutoLocalizeTextUI>();
        var oldText = smallDescLocalizer.Text;
        smallDescLocalizer.Text = s;
        return oldText;
    }

    internal CG.List<string> MissingMods(int saveSlot)
    {
        var syncedFilenameSuffix = ".json.dat";
        var saveDir = DataPaths.SaveDataDir(saveSlot);

        try
        {
            _managedInstance.LoadSaveData(saveSlot);
            var optionalMods = SaveData is null ? [] : SaveData.OptionalMods;
            SaveData = null;

            // The ?* instead of just * is to work around a quirk of EnumerateFiles;
            // see https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefiles?view=netstandard-2.1#system-io-directory-enumeratefiles(system-string-system-string)
            return IO
                .Directory.EnumerateFiles(
                    saveDir,
                    "?*" + syncedFilenameSuffix,
                    IO.SearchOption.AllDirectories
                )
                .Select(path =>
                {
                    var name = IO.Path.GetFileName(path);
                    return name.Substring(0, name.Length - syncedFilenameSuffix.Length);
                })
                .Where(modGUID =>
                {
                    return !Bep.Bootstrap.Chainloader.PluginInfos.ContainsKey(modGUID)
                        && !optionalMods.Contains(modGUID);
                })
                .ToList();
        }
        catch (IO.DirectoryNotFoundException)
        {
            return [];
        }
    }

    /// DataManager's implementation of <see cref="ISaveDataMod{T}"/>.
    public DataManagerSaveData? SaveData { get; set; }
}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public record DataManagerSaveData
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
{
    public CG.HashSet<string> OptionalMods { get; set; } = [];
}
