#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;

using GD_NET_ScOUT;

using Godot;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace GD_NET_ScOUT_TOOL;

[Tool]
public partial class Dock : BoxContainer
{
    internal const string ScenePath = "res://addons/GD_NET_ScOUT/dock.tscn";
    private const string TextSceneFileExtension = ".tscn";
    private const string MainSceneTestSetting = "application/run/main_scene.gdnetscout";

    private static readonly PackedScene ClearablePathContainer =
        GD.Load<PackedScene>("res://addons/GD_NET_ScOUT/clearable_path_container.tscn");

    [Export] private Button _hideAllButton;
    [Export] private VBoxContainer _dirContainer;
    [Export] private Button _addDirButton;
    [Export] private FileDialog _addDirDialog;
    [Export] private VBoxContainer _sceneFileContainer;
    [Export] private Button _addSceneFileButton;
    [Export] private FileDialog _addSceneFilesDialog;
    [Export] private CheckBox _verboseLoggingButton;
    [Export] private CheckBox _printReportsButton;
    [Export] private CheckBox _exportReportsButton;
    [Export] private TextEdit _testReportPath;
    [Export] private Button _saveAsButton;
    [Export] private FileDialog _saveAsDialog;
    [Export] private CheckBox _setProjectSettingButton;
    [Export] private RichTextLabel _messageLabel;

    private readonly ISet<string> _dirNames = new HashSet<string>();

    private readonly ISet<string> _sceneFileNames = new HashSet<string>();

    // Required to avoid https://github.com/godotengine/godot/issues/78513
    private readonly Dictionary<ulong, string> _removeButtonDict = new();

    public override void _Ready()
    {
        try
        {
            _hideAllButton.Pressed += OnHideAllButtonPressed;
            _addDirButton.Pressed += OnAddDirButtonPressed;
            _addSceneFileButton.Pressed += OnAddSceneFileButtonPressed;
            _addDirDialog.DirSelected += DirSelected;
            _addSceneFilesDialog.FileSelected += FileSelected;
            _addSceneFilesDialog.FilesSelected += FilesSelected;
            _exportReportsButton.Pressed += UpdateReportExportEnabled;
            _saveAsButton.Pressed += OnSaveAsButtonPressed;
            _saveAsDialog.FileSelected += SaveScene;
            UpdateSaveAsButton();
            UpdateReportExportEnabled();
            _messageLabel.Modulate = Colors.Gray;
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
    }

    public void OnHideAllButtonPressed()
    {
        foreach (Control control in _hideAllButton.GetParent()
            .GetParent()
            .GetChildren()
            .OfType<Control>()
            .Where(node => !node.Equals(_hideAllButton.GetParent())))
        {
            control.Visible = !_hideAllButton.ButtonPressed;
        }
        foreach (Control control in _hideAllButton.GetParent()
            .GetChildren()
            .OfType<Control>()
            .Where(node => !node.Equals(_hideAllButton)))
        {
            control.Visible = !_hideAllButton.ButtonPressed;
        }
    }

    public void OnAddDirButtonPressed() => _addDirDialog.Visible = true;
    public void OnAddSceneFileButtonPressed() => _addSceneFilesDialog.Visible = true;
    public void OnSaveAsButtonPressed() => _saveAsDialog.Visible = true;

    public void DirSelected(string dir)
    {
        AddPath(dir.EndsWith('/') ? dir : $"{dir}/", true, _dirContainer, _addDirButton);
    }

    public void FilesSelected(params string[] files)
    {
        foreach (string file in files)
        {
            FileSelected(file);
        }
    }

    public void FileSelected(string file)
    {
        if (!file.EndsWith(TextSceneFileExtension))
        {
            _messageLabel.Text = "[i]Test scenes must be *.tscn files[/i]";
            return;
        }

        AddPath(file, false, _sceneFileContainer, _addSceneFileButton);
    }

    private void AddPath(string path, bool isDir, VBoxContainer container, Button button)
    {
        if (!(isDir ? _dirNames : _sceneFileNames).Add(path))
        {
            _messageLabel.Text =
                isDir ? "[i]Directory already present[/i]" : "[i]Scene file(s) already present[/i]";
            return;
        }

        HBoxContainer clearablePathContainer = ClearablePathContainer.Instantiate<HBoxContainer>();
        clearablePathContainer.GetChildren().OfType<Label>().First().Text = path;
        container.AddChild(clearablePathContainer);
        container.MoveChild(clearablePathContainer, button.GetIndex());
        UpdateSaveAsButton();
        _removeButtonDict[clearablePathContainer.GetInstanceId()] = path;
        clearablePathContainer.GetChildren().OfType<Button>().First().Pressed += PathRemoved;
    }

    public void PathRemoved()
    {
        bool isDir = true;
        Button? pressedButton = _dirContainer.GetChildren()
            .OfType<BoxContainer>()
            .SelectMany(node => node.GetChildren())
            .OfType<Button>()
            .FirstOrDefault(button => button is {ToggleMode: true, ButtonPressed: true});
        if (pressedButton is null)
        {
            pressedButton = _sceneFileContainer.GetChildren()
                .OfType<BoxContainer>()
                .SelectMany(node => node.GetChildren())
                .OfType<Button>()
                .FirstOrDefault(button => button is {ToggleMode: true, ButtonPressed: true});
            isDir = false;
        }
        if (pressedButton is null
            || !_removeButtonDict.Remove(
                pressedButton.GetParent().GetInstanceId(), out string? path
            ))
        {
            return;
        }

        (isDir ? _dirNames : _sceneFileNames).Remove(path);
        pressedButton.GetParent().QueueFree();
        UpdateSaveAsButton();
    }

    private void UpdateSaveAsButton()
    {
        if (!_dirNames.Any() && !_sceneFileNames.Any())
        {
            if (_saveAsButton.Disabled)
            {
                return;
            }

            _saveAsButton.Disabled = true;
            _saveAsButton.TooltipText = "Choose at least one directory and/or at least one scene.";
            return;
        }

        if (!_saveAsButton.Disabled)
        {
            return;
        }

        _saveAsButton.Disabled = false;
        _saveAsButton.TooltipText = string.Empty;
    }

    public void UpdateReportExportEnabled()
    {
        _testReportPath.Editable = _exportReportsButton.ButtonPressed;
    }

    public void SaveScene(string path)
    {
        try
        {
            if (!path.EndsWith(TextSceneFileExtension))
            {
                _messageLabel.Text = "[i]Test scene runner must be saved as *.tscn file[/i]";
                return;
            }

            if (_sceneFileNames.Contains(path))
            {
                _messageLabel.Text =
                    "[i]Test scene runner cannot overwrite an included test scene[/i]";
                return;
            }

            string dir = path[..(path.LastIndexOf('/') + 1)];
            if (_dirNames.Contains(dir))
            {
                _messageLabel.Text =
                    "[i]Test scene runner cannot be saved in an included test scene directory[/i]";
                return;
            }

            PackedScene template = GD.Load<PackedScene>(TestSceneRunner.ScenePath);
            TestSceneRunner testSceneRunner = template.Instantiate<TestSceneRunner>();
            testSceneRunner.Scenes = _sceneFileNames.Select(GD.Load<PackedScene>).ToArray();
            testSceneRunner.TestSceneDirectories = _dirNames.ToArray();
            testSceneRunner.Verbose = _verboseLoggingButton.ButtonPressed;
            testSceneRunner.PrintReportToStdOut = _printReportsButton.ButtonPressed;
            testSceneRunner.SaveReportToFile = _exportReportsButton.ButtonPressed;
            testSceneRunner.TestReportPath = testSceneRunner.SaveReportToFile
                ? _testReportPath.Text
                : string.Empty;
            PackedScene scene = new PackedScene();
            Error result = scene.Pack(testSceneRunner);
            if (result != Error.Ok)
            {
                _messageLabel.Text = $"[i]Error packing scene: {result}[/i]";
                return;
            }

            result = ResourceSaver.Save(scene, path);
            if (result != Error.Ok)
            {
                _messageLabel.Text = $"[i]Error saving packed scene: {result}[/i]";
                return;
            }

            _messageLabel.Text =
                $"[i]Test scene runner saved to '{path}'\n\nNote: if already open, '{path}' should be manually reloaded to show changes[/i]";
            if (!_setProjectSettingButton.ButtonPressed)
            {
                return;
            }

            ProjectSettings.SetSetting(MainSceneTestSetting, path);
            ProjectSettings.Save();
            _messageLabel.Text +=
                $"\n\n[i]Project setting '{MainSceneTestSetting}' set to '{path}'[/i]";
        }
        catch (Exception e)
        {
            _messageLabel.Text = $"[i]Error: {e.Message}[/i]";
            throw;
        }
    }
}
#endif
