using System;
using Godot;

namespace ui.hud;

/// <summary>
/// A simple UI element to display a game control. If there are multiple options for a single control, they are separated by a slash.
/// </summary>
[Tool]
public partial class ControlHint : HBoxContainer
{
    private Texture2D[] _icons = Array.Empty<Texture2D>();
    private HBoxContainer _iconBox = null;
    private string _control = "";
    private Label _label = null;

    private HBoxContainer IconBox => _iconBox ??= GetNode<HBoxContainer>("Icons");
    private Label Label => _label ??= GetNode<Label>("Label");

    /// <summary>Icons indicating what maps to the control.</summary>
    [Export] public Texture2D[] Icons
    {
        get => _icons;
        set
        {
            _icons = value;
            foreach (Node child in IconBox.GetChildren())
            {
                IconBox.RemoveChild(child);
                child.QueueFree();
            }
            for (int i = 0; i < _icons.Length; i++)
            {
                if (i > 0)
                    IconBox.AddChild(new Label() { Text = "/" });
                TextureRect icon = new() { Texture = _icons[i], StretchMode = TextureRect.StretchModeEnum.KeepCentered };
                IconBox.AddChild(icon);
            }
        }
    }

    /// <summary>Name of the control. Is preceded by a colon (the value of this property does not include the colon).</summary>
    [Export] public string Control
    {
        get => _control;
        set
        {
            _control = value;
            Label.Text = $": {value}";
        }
    }
}
