using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierMoveMono), true)]
public class BezierMoveMonoInspector : Editor {
    private BezierMoveMono _inspector;

    protected void OnEnable() {
        _inspector = (BezierMoveMono)target;
    }

    public override void OnInspectorGUI() {
        _inspector.Segments = EditorGUILayout.IntField("段数", _inspector.Segments);
        _inspector.moveMode = (BezierMoveMode) EditorGUILayout.EnumPopup(_inspector.moveMode);


        switch (_inspector.moveMode) {
            case BezierMoveMode.FourPoint:
                _inspector.from = EditorGUILayout.Vector3Field("第一个控制点（起点）", _inspector.from);

                _inspector.one = EditorGUILayout.Vector3Field("第二个控制点", _inspector.one);
                _inspector.two = EditorGUILayout.Vector3Field("第三个控制点", _inspector.two);

                _inspector.to = EditorGUILayout.Vector3Field("第四个控制点（终点）", _inspector.to);

                if (GUILayout.Button("Start Move By Point")) {
                    _inspector.StartMoving(_inspector.from, _inspector.to, _inspector.one, _inspector.two);
                }
                break;
            case BezierMoveMode.AngleLength:
                _inspector.from = EditorGUILayout.Vector3Field("第一个控制点（起点）", _inspector.from);

                _inspector.angle1 = EditorGUILayout.FloatField("第二个控制点相对于起点终点连线垂直方向的角度", _inspector.angle1);
                _inspector.length1 = EditorGUILayout.FloatField("第二个控制点相对于起点终点连线垂直方向的角度延展的长度", _inspector.length1);

                _inspector.angle2 = EditorGUILayout.FloatField("第三个控制点相对于起点终点连线垂直方向的角度", _inspector.angle2);
                _inspector.length2 = EditorGUILayout.FloatField("第三个控制点相对于起点终点连线垂直方向的角度延展的长度", _inspector.length2);

                _inspector.to = EditorGUILayout.Vector3Field("第四个控制点（终点）", _inspector.to);

                if (GUILayout.Button("Start Move By Angle")) {
                    _inspector.StartMoving(_inspector.from, _inspector.to);
                }
                break;
            case BezierMoveMode.RandomFourPoint:
                _inspector.from = EditorGUILayout.Vector3Field("第一个控制点（起点）", _inspector.from);

                EditorGUILayout.LabelField($"第二个控制点 : {_inspector.one}");
                EditorGUILayout.LabelField($"第三个控制点 : {_inspector.two}");

                _inspector.randomRange = EditorGUILayout.Vector3Field("控制点抖动随机范围", _inspector.randomRange);

                _inspector.to = EditorGUILayout.Vector3Field("第四个控制点（终点）", _inspector.to);

                if (GUILayout.Button("Random Move")) {
                    _inspector.StartMovingRandom(_inspector.from, _inspector.to);
                }
                break;
            default:
                break;
        }
    }

    void OnSceneGUI() {

        // 当前点的世界坐标  
        Vector3 point1 = _inspector.one;
        Vector3 point2 = _inspector.two;

        // 使用 Handles.PositionHandle 显示一个可拖动的点  
        EditorGUI.BeginChangeCheck();
        Vector3 newPoint1 = Handles.PositionHandle(point1, Quaternion.identity);
        if (EditorGUI.EndChangeCheck()) {
            // 记录更改以支持撤销操作  
            Undo.RecordObject(_inspector, "Move Point");

            // 更新脚本中的点位置变量  
            _inspector.one = newPoint1;

            // 标记对象已更改  
            EditorUtility.SetDirty(_inspector);

            _inspector.StartMoving(_inspector.from, _inspector.to, _inspector.one, _inspector.two);
        }

        // 使用 Handles.PositionHandle 显示一个可拖动的点  
        EditorGUI.BeginChangeCheck();
        Vector3 newPoint2 = Handles.PositionHandle(point2, Quaternion.identity);
        if (EditorGUI.EndChangeCheck()) {
            // 记录更改以支持撤销操作  
            Undo.RecordObject(_inspector, "Move Point");

            // 更新脚本中的点位置变量  
            _inspector.two = newPoint2;

            // 标记对象已更改  
            EditorUtility.SetDirty(_inspector);

            _inspector.StartMoving(_inspector.from, _inspector.to, _inspector.one, _inspector.two);
        }
    }
}
