using System;
using UnityEngine;

public class CycleTimer : Timer {
    public float Cycle = 1;

    public Action onCycleDelegate;

    float cycleElasedTime = 0f;

    public CycleTimer(string name, bool isUnique, bool isUnscaled, float cycle, Action onCycleDelegate, Action cdReset) : base(name, isUnique, isUnscaled, cdReset) {
        Cycle = cycle;
        this.onCycleDelegate = onCycleDelegate;
    }

    public override void Process(float deltaTime) {
        if (pause) {
            return;
        }

        base.Process(deltaTime);

        cycleElasedTime += deltaTime;

        if (cycleElasedTime >= Cycle) {
            cycleElasedTime = 0;
            onCycleDelegate?.Invoke();
        }
    }

    public override void Reset() {
        base.Reset();
        cycleElasedTime = 0;
    }
}