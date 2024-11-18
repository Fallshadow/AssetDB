using System;
using UnityEngine;

[Serializable]
public class CountDownUpdateTimer : Timer {

    private readonly Action<float> onUpdateDelegate;
    private readonly Action onFinishDelegate;
    private readonly bool autoRemove = true;
    public float countDownTime = -1f;
    
    public CountDownUpdateTimer(string name, bool isUnique, bool isUnscaled, float countDownTime, bool autoRemove, Action<float> onUpdateDelegate, Action onFinishDelegate) : base(name, isUnique, isUnscaled) {
        this.onUpdateDelegate = onUpdateDelegate;
        this.onFinishDelegate = onFinishDelegate;
        this.autoRemove = autoRemove;
        this.countDownTime = countDownTime;
    }

    public override void Process(float deltaTime) {
        if (pause)
            return;

        base.Process(deltaTime);

        if (countDownTime > 0 && GetElasedTime() >= countDownTime) {
            if (autoRemove) {
                GameMgr.time.RemoveTimer(this);
            }
            else {
                pause = true;
            }

            onFinishDelegate?.Invoke();
        }
        
        if (deltaTime != 0) {
            onUpdateDelegate?.Invoke(deltaTime);
        }
    }
}
