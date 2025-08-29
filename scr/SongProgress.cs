using Godot;
using System;

public partial class SongProgress : HSlider
{
    Tween tween;
    public void start(int duration) {
        tween?.Kill();
        tween = CreateTween();

        GD.Print(duration);

        tween.TweenProperty(
            this, HSlider.PropertyName.Value.ToString(),
            MaxValue, duration/1000
        )
        .From(0);
    }

    public void stop() {
        tween?.Pause();
    }

    public void resume() {
        tween.Play();
    }
}
