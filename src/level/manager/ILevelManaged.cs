namespace level.manager;

/// <summary>Interface for objects managed by a <c>LevelManager</c>, which provides it information about the level.</summary>
public interface ILevelManaged
{
    /// <summary>The <c>LevelManager</c> in which this object is instantiated and is managing the level.</summary>
    public LevelManager LevelManager { get; }
}