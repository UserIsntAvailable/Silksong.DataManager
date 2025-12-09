namespace Silksong.DataManager;

/// Interface for mods that need to store save data.
public interface ISaveDataMod<T> : ISaveDataMod
    where T : class
{
    /// If there are errors while loading the data (eg. no such data exists), or the game is at the
    /// title screen, this property is set to null.
    T? SaveData { get; set; }

    bool IsOptional => false;

    System.Type ISaveDataMod.SaveDataType => typeof(T);

    object? ISaveDataMod.UntypedSaveData
    {
        get => SaveData;
        set => SaveData = value == null ? null : (T)value;
    }

    bool ISaveDataMod.UntypedIsOptional => IsOptional;
}

/// An implementation detail that must be made public due to accessibility rules.
/// Client mods should instead implement <see cref="ISaveDataMod{T}"/>.
public interface ISaveDataMod
{
    /// The target type to use when deserializing save data for this mod.
    System.Type SaveDataType { get; }

    /// The object (of type <see cref="SaveDataType"/>) to be serialized.
    object? UntypedSaveData { get; set; }

    bool UntypedIsOptional { get; }
}
