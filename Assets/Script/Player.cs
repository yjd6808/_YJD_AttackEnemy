using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class Player : MonoBehaviour
{
    // FanAttack 관련 변수
    public float fanRadious_ = 5.0f; // 부채꼴 반지름 길이
    public float fanDegree_ = 45.0f; // 부채꼴 각도
    public float fanAttackDistance_ = 0.0f; // 이 공격이 캐릭터 중심으로부터 얼만큼 떨어진 위치에서부터 타격되는지

    // SquareAttack 관련 변수
    public float squareWidthLength_ = 3.0f; // 캐릭터가 바라보는 방향의 사각형 변 길이
    public float squareHeightLength_ = 5.0f; // 캐릭터가 바라보는 방향의 사각형 변 길이
    public float squareAttackDistance_ = 3.0f; // 이 공격이 캐릭터 중심으로부터 얼만큼 떨어진 위치에서부터 타격되는지
    public Vector2 squrePivot_ = new (0.5f, 0.0f);

    // CircleAttack 관련 변수
    public float circleRadious_ = 5.0f; // 반지름 길이
    public float circleAttackDistance_ = 3.0f;  // 이 공격이 캐릭터 중심으로부터 얼만큼 떨어진 위치에서부터 타격되는지

    private Transform transform_;
    
    void Start()
    {
        transform_ = GetComponent<Transform>();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Alpha1))       // 키 패드말고 숫
        {
            FanAttack();
        }
        else if (Input.GetKey(KeyCode.Alpha2))
        {
            SquareAttack(1);
        }
        else if (Input.GetKey(KeyCode.Alpha3))
        {
            CircleAttack();
        }
        else if (Input.GetKey(KeyCode.Alpha4))
        {
            SquareAttack(2);
        }
    }

    // 플레이어가 바라보는 방향으로부터 _distance거리만큼 떨어진 지점의 위치
    Vector3 CalcAttackStartPos(float _distance) => transform_.position + transform_.forward * _distance;

    // a와 b사이의 거리 계산
    float CalcDistance(Vector3 _a, Vector3 _b) => (_a - _b).magnitude;    

    void FanAttack()
    {
        Vector3 attackStartPos = CalcAttackStartPos(fanAttackDistance_);
        List<GameObject> enemies = GameObject.FindGameObjectsWithTag("Enemy").ToList();

        foreach (var enemy in enemies)
        {
            Transform enemyTransform = enemy.GetComponent<Transform>();
            float distance = CalcDistance(enemyTransform.position, attackStartPos);

            // 타격점으로부터 거리내에 있어야함.
            if (distance >= fanRadious_)
                continue;

            // 타격점기준에서 바라보는 방향의 각도내에 적이 있어야함.
            Vector3 directionToEnmey = (enemyTransform.position - attackStartPos).normalized; // 타격점 기준에서 적이 위치하는 방향
            float radianToEnemy = Vector3.Dot(transform_.forward, directionToEnmey); // 내가 바라보는 방향과, 타격점 기준에서 적이 위치하는 방향을 내적하여 사이 각도를 계산한다.
            float degreeToEnemy = RadianToDegree(MathF.Acos(radianToEnemy));

            // 크기로 굳이 나눠주지 않는 이유는 내적 계산 함수의 인자로 전달한 벡터들이 모두 방향 벡터(길이가 1)인 벡터이므로 두 벡터 크기의 곱으로 나눠줄 필요가 없다.
            if (degreeToEnemy <= fanDegree_ / 2.0f)
            {
                ProcessHit(enemy);
            }
        }
    }

    Vector3 RotateDirectionVector(Vector3 _dir, float _degree)
    {
        Quaternion rotation = Quaternion.AngleAxis(_degree, Vector3.up);
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(rotation);
        return rotationMatrix.MultiplyPoint(_dir);
    }

    // _target에 _positions로 이뤄진 다각형 내부의 점인지 판단한다.
    bool IsCCW(Vector3 _target, params Vector3[] _positions)
    {
        int numPoints = _positions.Length;

        // 다각형의 각 점을 이용하여 면적 계산
        // 만약 점1, 점2, 점3, 점4로 구성된 사각형인 경우
        // _target -> 점1 벡터와, 점1 -> 점2의 외적값이 up vector인 경우
        // _target -> 점2 벡터와, 점2 -> 점3의 외적값이 up vector인 경우
        // _target -> 점3 벡터와, 점3 -> 점4의 외적값이 up vector인 경우
        // _target -> 점4 벡터와, 점4 -> 점1의 외적값이 up vector인 경우를 모두 충족한다면 다각형 내부에 점이 존재한다는 뜻이다.
        for (int i = 0; i < numPoints; i++)
        {
            Vector3 currentPoint = _positions[i];
            Vector3 nextPoint = _positions[(i + 1) % numPoints]; // 다음 점 (마지막 점에서는 첫 번째 점으로 순환)

            Vector3 targetToCurrent = currentPoint - _target;   // target에서 current 방향의 벡터
            Vector3 currentToNext = nextPoint - currentPoint;   // current에서 target 방향의 벡터

            // targetToCurrent벡터에서 currentToNext가 왼쪽에 있지 않는 경우 CCW(반시계)가 아닌 CW이므로 내부에 있지 않다고 판단한다.
            // 4개의 외적값이 모두 같은 부호인지만 체크해도 된다.
            if (Vector3.Cross(targetToCurrent, currentToNext).y > 0)    
                return false;
        }
        return true;
    }

    void SquareAttack(int _version)
    {
        Vector3 attackStartPos = CalcAttackStartPos(squareAttackDistance_);
        List<GameObject> enemies = GameObject.FindGameObjectsWithTag("Enemy").ToList();

        Vector3 forwardDir = transform_.forward;
        Vector3 rightDir = RotateDirectionVector(forwardDir, 90.0f);   // transform_.right와 같다...

        if (_version == 1)
        {
            Vector3 leftBottom = attackStartPos +
                                 rightDir * (0 - squrePivot_.x) * squareWidthLength_ +
                                 forwardDir * (0 - squrePivot_.y) * squareHeightLength_;
            //Vector3 rightBottom = attackStartPos +
            //                      rightDir * (1 - squrePivot_.x) * squareWidthLength_ +
            //                      forwardDir * (0 - squrePivot_.y) * squareHeightLength_;
            //Vector3 leftTop = attackStartPos +
            //                  rightDir * (0 - squrePivot_.x) * squareWidthLength_ +
            //                  forwardDir * (1 - squrePivot_.y) * squareHeightLength_; 
            //Vector3 rightTop = attackStartPos +
            //                   rightDir * (1 - squrePivot_.x) * squareWidthLength_ + 
            //                   forwardDir * (1 - squrePivot_.y) * squareHeightLength_;
            // 위 코드와 동일함
            Vector3 rightBottom = leftBottom + rightDir * squareWidthLength_;
            Vector3 leftTop = leftBottom + forwardDir * squareHeightLength_;
            Vector3 rightTop = leftBottom + rightDir * squareWidthLength_ + forwardDir * squareHeightLength_;

            foreach (var enemy in enemies)
            {
                Transform enemyTransform = enemy.GetComponent<Transform>();

                if (!IsCCW(enemyTransform.position, leftBottom, rightBottom, rightTop, leftTop))
                    continue;

                ProcessHit(enemy);
            }
        }
        else
        {
            float rangeMinX = squareWidthLength_ * squrePivot_.x;
            float rangeMaxX = squareWidthLength_ * (1 - squrePivot_.x);
            float rangeMinY = squareHeightLength_ * squrePivot_.y;
            float rangeMaxY = squareHeightLength_ * (1 - squrePivot_.y);

            foreach (var enemy in enemies)
            {
                Transform enemyTransform = enemy.GetComponent<Transform>();

                Vector3 posToEnemy = enemyTransform.position - attackStartPos;

                float radToEnemyX = MathF.Acos(Vector3.Dot(posToEnemy.normalized, rightDir));
                float localX = MathF.Cos(radToEnemyX) * posToEnemy.magnitude;

                float radToEnemyY = MathF.Acos(Vector3.Dot(forwardDir, posToEnemy.normalized));
                float localY = MathF.Cos(radToEnemyY) * posToEnemy.magnitude;

                if (localX >= rangeMinX && localX <= rangeMaxX && localY >= rangeMinY && localY <= rangeMaxY)
                {
                    ProcessHit(enemy);
                }
            }
        }

    }

    void CircleAttack()
    {
        Vector3 attackStartPos = CalcAttackStartPos(circleAttackDistance_);
        List<GameObject> enemies = GameObject.FindGameObjectsWithTag("Enemy").ToList();

        foreach (var enemy in enemies)
        {
            Transform enemyTransform = enemy.GetComponent<Transform>();
            float distance = CalcDistance(enemyTransform.position, attackStartPos);

            // 타격점으로부터 거리내에 있어야함.
            if (distance >= circleRadious_)
                continue;

            ProcessHit(enemy);
        }
    }

    void ProcessHit(GameObject _enemy)
    {
        Enemy enemyComponent = _enemy.GetComponent<Enemy>();
        if (enemyComponent == null)
            return;

        enemyComponent.Hit();
    }

    float RadianToDegree(float _radian)
    {
        return _radian * 180.0f / (float)Math.PI;
    }
}
