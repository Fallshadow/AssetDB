using System.Collections;
using UnityEngine;

public class ProgressModule : ModuleBase {

    public override void OnInit() {
        GameEvents.gameInited += OnGameInited;
    }

    private void OnGameInited() {
        GameMgr.ui.Open<LoginUI>();
    }
}