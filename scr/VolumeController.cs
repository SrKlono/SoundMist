using Godot;
using System;

public partial class VolumeController : Button
{
    [Signal] public delegate void SoundValueChangedEventHandler(float value);
    [Signal] public delegate void DragEndedEventHandler(float value); 

    VSlider slider;
    ProgressBar progressBar;
    Control leaveBox;

    public override void _Ready()
    {
        slider = GetNode<VSlider>("VSlider");
        progressBar = slider.GetNode<ProgressBar>("ProgressBar");
        leaveBox = slider.GetNode<Control>("LeaveBox");

        slider.Visible = false;

        slider.ValueChanged += (changed) => {
            progressBar.Value = slider.Value;
            EmitSignal(VolumeController.SignalName.SoundValueChanged, slider.Value/100);
        };
        slider.DragEnded += (_) => EmitSignal(VolumeController.SignalName.DragEnded, slider.Value/100);
        
        MouseEntered += () => slider.Visible = true;
        leaveBox.MouseExited += () => slider.Visible = false;
    }

    public void setVolume(float value) {
        slider.Value = value;
    }
}
