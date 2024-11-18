using System;

[System.Serializable]
public class UpdateTimer : Timer {
    private readonly Action<float> onUpdateDelegate;

    public UpdateTimer(string name, bool isUnique, bool isUnscaled, Action<float> onUpdateDelegate) : base(name, isUnique, isUnscaled) {
        this.onUpdateDelegate = onUpdateDelegate;
    }

    public override void Process(float deltaTime) {
        if (pause)
            return;

        base.Process(deltaTime);

        if (deltaTime != 0) {
            onUpdateDelegate?.Invoke(deltaTime);
        }
    }
}
