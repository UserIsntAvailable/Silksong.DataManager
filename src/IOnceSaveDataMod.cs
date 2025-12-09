namespace Silksong.DataManager;

/// Interface for mods that need to store some save data at the start of each new game,
/// but don't need to change it afterwards.
public interface IOnceSaveDataMod<T> : IOnceSaveDataMod
    where T : class
{
    /// The once-save data for the current file.
    /// DataManager reads and stores its value once at the start of each new game,
    /// after GameManager.StartNewGame completes.
    /// If this property is null at that time, no data are written for that file for the client mod.
    /// Upon loading any existing file, this property is set to the saved data for that file
    /// for the client mod, if any exists.
    /// If there are errors while loading the data (eg. no such data exists), or the game is at the
    /// title screen, this property is set to null.
    T? OnceSaveData { get; set; }

    bool IsOptional => false;

    System.Type IOnceSaveDataMod.OnceSaveDataType => typeof(T);

    object? IOnceSaveDataMod.UntypedOnceSaveData
    {
        get => OnceSaveData;
        set => OnceSaveData = value == null ? null : (T)value;
    }
}

/// An implementation detail that must be made public due to accessibility rules.
/// Client mods should instead implement <see cref="IOnceSaveDataMod{T}"/>.
public interface IOnceSaveDataMod
{
    /// The target type to use when deserializing save data for this mod.
    System.Type OnceSaveDataType { get; }

    /// The object (of type <see cref="OnceSaveDataType"/>) to be serialized.
    object? UntypedOnceSaveData { get; set; }

    bool UntypedIsOptional { get; }
}
