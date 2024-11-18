/// <summary>
/// 提供给不同模块之间传递讯息，以降低耦合度。
/// 事件分类以发送者为依据，主要是各个管理系统
/// </summary>
public enum EventGroup : short {
    NONE = 0,
    NETWORK,
    INPUT,
    UI,
    SYSTEM,
    BATTLE,
    CAMERA,
}

public enum CameraEvent : short {
    NONE = 0,
    ROTATE,
}