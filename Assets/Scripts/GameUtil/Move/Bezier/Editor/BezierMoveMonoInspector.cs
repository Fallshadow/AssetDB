using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierMoveMono), true)]
public class BezierMoveMonoInspector : Editor {
    private BezierMoveMono _inspector;

    protected void OnEnable() {
        _inspector = (BezierMoveMono)target;
    }

    public override void OnInspectorGUI() {
        _inspector.Segments = EditorGUILayout.IntField("����", _inspector.Segments);
        _inspector.moveMode = (BezierMoveMode) EditorGUILayout.EnumPopup(_inspector.moveMode);


        switch (_inspector.moveMode) {
            case BezierMoveMode.FourPoint:
                _inspector.from = EditorGUILayout.Vector3Field("��һ�����Ƶ㣨��㣩", _inspector.from);

                _inspector.one = EditorGUILayout.Vector3Field("�ڶ������Ƶ�", _inspector.one);
                _inspector.two = EditorGUILayout.Vector3Field("���������Ƶ�", _inspector.two);

                _inspector.to = EditorGUILayout.Vector3Field("���ĸ����Ƶ㣨�յ㣩", _inspector.to);

                if (GUILayout.Button("Start Move By Point")) {
                    _inspector.StartMoving(_inspector.from, _inspector.to, _inspector.one, _inspector.two);
                }
                break;
            case BezierMoveMode.AngleLength:
                _inspector.from = EditorGUILayout.Vector3Field("��һ�����Ƶ㣨��㣩", _inspector.from);

                _inspector.angle1 = EditorGUILayout.FloatField("�ڶ������Ƶ����������յ����ߴ�ֱ����ĽǶ�", _inspector.angle1);
                _inspector.length1 = EditorGUILayout.FloatField("�ڶ������Ƶ����������յ����ߴ�ֱ����ĽǶ���չ�ĳ���", _inspector.length1);

                _inspector.angle2 = EditorGUILayout.FloatField("���������Ƶ����������յ����ߴ�ֱ����ĽǶ�", _inspector.angle2);
                _inspector.length2 = EditorGUILayout.FloatField("���������Ƶ����������յ����ߴ�ֱ����ĽǶ���չ�ĳ���", _inspector.length2);

                _inspector.to = EditorGUILayout.Vector3Field("���ĸ����Ƶ㣨�յ㣩", _inspector.to);

                if (GUILayout.Button("Start Move By Angle")) {
                    _inspector.StartMoving(_inspector.from, _inspector.to);
                }
                break;
            case BezierMoveMode.RandomFourPoint:
                _inspector.from = EditorGUILayout.Vector3Field("��һ�����Ƶ㣨��㣩", _inspector.from);

                EditorGUILayout.LabelField($"�ڶ������Ƶ� : {_inspector.one}");
                EditorGUILayout.LabelField($"���������Ƶ� : {_inspector.two}");

                _inspector.randomRange = EditorGUILayout.Vector3Field("���Ƶ㶶�������Χ", _inspector.randomRange);

                _inspector.to = EditorGUILayout.Vector3Field("���ĸ����Ƶ㣨�յ㣩", _inspector.to);

                if (GUILayout.Button("Random Move")) {
                    _inspector.StartMovingRandom(_inspector.from, _inspector.to);
                }
                break;
            default:
                break;
        }
    }

    void OnSceneGUI() {

        // ��ǰ�����������  
        Vector3 point1 = _inspector.one;
        Vector3 point2 = _inspector.two;

        // ʹ�� Handles.PositionHandle ��ʾһ�����϶��ĵ�  
        EditorGUI.BeginChangeCheck();
        Vector3 newPoint1 = Handles.PositionHandle(point1, Quaternion.identity);
        if (EditorGUI.EndChangeCheck()) {
            // ��¼������֧�ֳ�������  
            Undo.RecordObject(_inspector, "Move Point");

            // ���½ű��еĵ�λ�ñ���  
            _inspector.one = newPoint1;

            // ��Ƕ����Ѹ���  
            EditorUtility.SetDirty(_inspector);

            _inspector.StartMoving(_inspector.from, _inspector.to, _inspector.one, _inspector.two);
        }

        // ʹ�� Handles.PositionHandle ��ʾһ�����϶��ĵ�  
        EditorGUI.BeginChangeCheck();
        Vector3 newPoint2 = Handles.PositionHandle(point2, Quaternion.identity);
        if (EditorGUI.EndChangeCheck()) {
            // ��¼������֧�ֳ�������  
            Undo.RecordObject(_inspector, "Move Point");

            // ���½ű��еĵ�λ�ñ���  
            _inspector.two = newPoint2;

            // ��Ƕ����Ѹ���  
            EditorUtility.SetDirty(_inspector);

            _inspector.StartMoving(_inspector.from, _inspector.to, _inspector.one, _inspector.two);
        }
    }
}
