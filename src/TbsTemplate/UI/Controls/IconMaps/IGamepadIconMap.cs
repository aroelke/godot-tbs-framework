using Godot;

namespace TbsTemplate.UI.Controls.IconMaps;

public interface IGamepadAxisIconMap
{
    public Texture2D LeftAxis { get; set; }

    public Texture2D RightAxis { get; set; }
}

public interface IGamepadButtonIconMap
{
    public Texture2D Dpad { get; set; }
}