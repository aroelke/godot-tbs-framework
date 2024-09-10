using Godot;
using TbsTemplate.UI.Controls.Device;
using TbsTemplate.UI.Controls.Icons.Generic;

namespace TbsTemplate.UI.Controls.Icons;

/// <summary>Resource mapping mouse actions onto icons to display for them.</summary>
[GlobalClass, Tool]
public partial class MouseIconMap : IndividualIconMap<MouseButton>
{
    public override Texture2D this[StringName action] { get => this[InputManager.GetInputMouseButton(action)]; set => this[InputManager.GetInputMouseButton(action)] = value; }

    /// <summary>Icon to display for mouse motion.</summary>
    [Export] public Texture2D Motion = null;

    /// <summary>Left click icon.</summary>
    [Export] public Texture2D Left
    {
        get => this[MouseButton.Left];
        set => this[MouseButton.Left] = value;
    }

    /// <summary>Right click icon.</summary>
    [Export] public Texture2D Right
    {
        get => this[MouseButton.Right];
        set => this[MouseButton.Right] = value;
    }

    public override bool ContainsKey(StringName action) => ContainsKey(InputManager.GetInputMouseButton(action));
}