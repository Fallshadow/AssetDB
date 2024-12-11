using System;
using Unity.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
public enum BezierMoveMode
{
    FourPoint,
    AngleLength,
    RandomFourPoint,
}

public class BezierMoveMono : MonoBehaviour {
    public BezierMoveMode moveMode;
    public int Segments = 60;
    public float angle1;
    public float angle2;
    public float length1;
    public float length2;
    public Vector3 randomRange;
    public int curIndex;
    public bool isMoveing;


    public Vector3 from;
    public Vector3 to;
    public Vector3 one;
    public Vector3 two;
    [NonSerialized]
    public NativeArray<float3> pathPoints;

    public void StartMoving(Vector3 from, Vector3 to) {
        this.from = from;
        this.to = to;

        pathPoints = new NativeArray<float3>(Segments, Allocator.Persistent);
        BezierCurve.EvaluateBezierWithAngle(from, to, angle1, angle2, length1, length2, Segments, ref pathPoints, out float length);
        curIndex = 0;
        isMoveing = true;
    }

    public void StartMoving(Vector3 from, Vector3 to, Vector3 one, Vector3 two) {
        this.from = from;
        this.to = to;
        this.one = one;
        this.two = two;

        pathPoints = new NativeArray<float3>(Segments, Allocator.Persistent);
        BezierCurve.EvaluateBezier(from, one, two, to, Segments, ref pathPoints, out float length);
        curIndex = 0;
        isMoveing = true;
    }

    public void StartMovingRandom(Vector3 from, Vector3 to) {
        this.from = from;
        this.to = to;
        one = randomCuboid(from, to);
        two = randomCuboid(one, to);
        one = new Vector3(
            one.x + Random.Range(-randomRange.x, randomRange.x),
            Mathf.Abs(one.y + Random.Range(-randomRange.y, randomRange.y)),
            one.z + Random.Range(-randomRange.z, randomRange.z));
        two = new Vector3(
            two.x + Random.Range(-randomRange.x, randomRange.x),
            Mathf.Abs(two.y + Random.Range(-randomRange.y, randomRange.y)),
            two.z + Random.Range(-randomRange.z, randomRange.z));

        StartMoving(from, to, one, two);
    }

    private Vector3 randomCuboid(Vector3 corner1, Vector3 corner2) {
        float randomX = Random.Range(corner1.x, corner2.x);
        float randomY = Random.Range(corner1.y, corner2.y);
        float randomZ = Random.Range(corner1.z, corner2.z);

        return new Vector3(randomX, randomY, randomZ);
    }

    private void FixedUpdate() {
        if (!isMoveing)
        {
            return;
        }
        int lastIndex = pathPoints.Length - 1;

        if (curIndex > lastIndex) {
            isMoveing = false;
            return;
        }

        Vector3 curPos = pathPoints[curIndex];
        var dir = (curPos - transform.position).normalized;

        transform.forward = dir;
        transform.position = pathPoints[curIndex];

        curIndex++;
    }

    private void OnDrawGizmos() {
        for (int i = 1; i < pathPoints.Length; i++) {
            Debug.DrawLine(pathPoints[i - 1], pathPoints[i], Color.green);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(one, 0.1f);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(two, 0.1f);
    }
}