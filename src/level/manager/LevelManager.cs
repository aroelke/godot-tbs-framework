using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using level.map;

namespace level.manager;

/// <summary>
/// Manages the setup of and objects inside a level and provides them information about it.  Is "global" in a way in that
/// all objects in the level may be able to see it and request information from it, but each level has its own manager.
/// </summary>
[Tool]
public partial class LevelManager : Node2D
{
    public override string[] _GetConfigurationWarnings()
    {
        List<string> warnings = new(base._GetConfigurationWarnings() ?? Array.Empty<string>());

        int maps = GetChildren().Where((c) => c is LevelMap).Count();
        if (maps < 1)
            warnings.Add("Level does not contain a map.");
        else if (maps > 1)
            warnings.Add($"Level contains too many maps ({maps}).");

        return warnings.ToArray();
    }
}