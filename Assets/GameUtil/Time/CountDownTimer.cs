using System;

public class CountDownTimer : Timer {
    private float countDownTime = 0f;

    private readonly Action onFinishDelegate;

    private readonly bool autoRemove = true;

    public CountDownTimer(string name, bool isUnique, bool isUnscaled, float countDownTime, Action onFinishDelegate, Action cbReset, bool autoRemove) : base(name, isUnique, isUnscaled, cbReset) {
        this.countDownTime = countDownTime;
        this.onFinishDelegate = onFinishDelegate;
        this.autoRemove = autoRemove;
    }

    public override void Process(float deltaTime) {
        if (pause) {
            return;
        }

        base.Process(deltaTime);
        if (GetElasedTime() >= countDownTime) {
            if (autoRemove) {
                GameMgr.time.RemoveTimer(this);
            }
            else {
                pause = true;
            }
            onFinishDelegate?.Invoke();

        }
    }

    public void Execute() {
        GameMgr.time.RemoveTimer(this);
        onFinishDelegate?.Invoke();
    }

    public override bool IsPlaying() {
        if (pause) {
            return false;
        }

        return GetElasedTime() <= countDownTime;
    }

    public void UpdateCountDown(float cd) {
        countDownTime = cd;
    }

    public float GetRemainTime() {
        return countDownTime - GetElasedTime();
    }
}