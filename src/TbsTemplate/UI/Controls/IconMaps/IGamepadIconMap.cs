using Godot;

namespace TbsTemplate.UI.Controls.IconMaps;

/// <summary>Interface defining special icons for gamepad axis icon maps.</summary>
public interface IGamepadAxisIconMap
{
    /// <summary>Icon to use for the gamepad left axis not pressed in any particular direction.</summary>
    public Texture2D LeftAxis { get; set; }

    /// <summary>Icon to use for the gamepad right axis not pressed in any particular direction.</summary>
    public Texture2D RightAxis { get; set; }
}

/// <summary>Interface defining special icons for gamepad button icon maps.</summary>
public interface IGamepadButtonIconMap
{
    /// <summary>Icon to use for the directional pad not pressed in any particular direction.</summary>
    public Texture2D Dpad { get; set; }
}