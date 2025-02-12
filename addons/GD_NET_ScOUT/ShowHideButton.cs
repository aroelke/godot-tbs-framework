using Godot;

namespace GD_NET_ScOUT;

public partial class ShowHideButton : Button
{
    [Export] private Control[]? _elements;

    public bool ElementsVisible { get; private set; } = true;

    public void OnButtonPressed()
    {
        ElementsVisible = !ElementsVisible;
        Text = ElementsVisible ? " _ " : "+";
        if (_elements is null)
        {
            return;
        }

        foreach (Control element in _elements)
        {
            element.Visible = ElementsVisible;
        }
    }
}
