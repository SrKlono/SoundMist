using Godot;
using System;

public partial class SongProgress : HSlider
{
    [Signal] public delegate void skipSongToValueEventHandler(float value);

    Tween tween;
    ProgressBar progressBar;
    double previousValue;
    int currentDuration;

    public override void _Ready()
    {
        progressBar = GetNode<ProgressBar>("ProgressBar");

        DragStarted += () =>
        {
            previousValue = Value;
            pause();
            ValueChanged += syncValues;
        };

        DragEnded += jumpSongToValue;
    }

    public void start(int duration, double startFromPercentage = 0)
    {
        tween?.Kill();
        tween = CreateTween();

        currentDuration = duration;
        duration /= 1000;

        tween.TweenProperty(
            progressBar, ProgressBar.PropertyName.Value.ToString(),
            progressBar.MaxValue, duration - duration * (startFromPercentage / 100)
        )
        .From(startFromPercentage);

        tween.Parallel();

        tween.TweenProperty(
            this, SongProgress.PropertyName.Value.ToString(),
            this.MaxValue, duration - duration * (startFromPercentage / 100)
        )
        .From(startFromPercentage);
    }

    private void jumpSongToValue(bool valueChanged)
    {
        ValueChanged -= syncValues;
        if (valueChanged)
        {
            EmitSignal(SongProgress.SignalName.skipSongToValue, Value / 100);
            start(currentDuration, Value);
        }
    }

    private void syncValues(double value)
    {
        progressBar.Value = value;
    }

    public void stop()
    {
        tween?.Stop();
        progressBar.Value = 0;
        Value = 0;
    }

    public void pause()
    {
        tween?.Pause();
    }

    public void resume()
    {
        tween.Play();
    }
}
