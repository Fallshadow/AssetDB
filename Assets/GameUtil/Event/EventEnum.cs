/// <summary>
/// �ṩ����ͬģ��֮�䴫��ѶϢ���Խ�����϶ȡ�
/// �¼������Է�����Ϊ���ݣ���Ҫ�Ǹ�������ϵͳ
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