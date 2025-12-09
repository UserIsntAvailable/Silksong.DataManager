namespace Silksong.DataManager;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public interface ISaveDataMod<T> : ISaveDataMod
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    where T : class
{
    /// If there are errors while loading the data (eg. no such data exists), or the game is at the
    /// title screen, this property is set to null.
    T? SaveData { get; set; }

    bool IsRequired => true;

    System.Type ISaveDataMod.SaveDataType => typeof(T);

    object? ISaveDataMod.UntypedSaveData
    {
        get => SaveData;
        set => SaveData = value == null ? null : (T)value;
    }
}

/// An implementation detail that must be made public due to accessibility rules.
/// Client mods should instead implement <see cref="ISaveDataMod{T}"/>.
public interface ISaveDataMod
{
    /// The target type to use when deserializing save data for this mod.
    System.Type SaveDataType { get; }

    /// The object (of type <see cref="SaveDataType"/>) to be serialized.
    object? UntypedSaveData { get; set; }
}
