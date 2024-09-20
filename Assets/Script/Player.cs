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
    // FanAttack ���� ����
    public float fanRadious_ = 5.0f; // ��ä�� ������ ����
    public float fanDegree_ = 45.0f; // ��ä�� ����
    public float fanAttackDistance_ = 0.0f; // �� ������ ĳ���� �߽����κ��� ��ŭ ������ ��ġ�������� Ÿ�ݵǴ���

    // SquareAttack ���� ����
    public float squareWidthLength_ = 3.0f; // ĳ���Ͱ� �ٶ󺸴� ������ �簢�� �� ����
    public float squareHeightLength_ = 5.0f; // ĳ���Ͱ� �ٶ󺸴� ������ �簢�� �� ����
    public float squareAttackDistance_ = 3.0f; // �� ������ ĳ���� �߽����κ��� ��ŭ ������ ��ġ�������� Ÿ�ݵǴ���
    public Vector2 squrePivot_ = new (0.5f, 0.0f);

    // CircleAttack ���� ����
    public float circleRadious_ = 5.0f; // ������ ����
    public float circleAttackDistance_ = 3.0f;  // �� ������ ĳ���� �߽����κ��� ��ŭ ������ ��ġ�������� Ÿ�ݵǴ���

    private Transform transform_;
    
    void Start()
    {
        transform_ = GetComponent<Transform>();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Alpha1))       // Ű �е帻�� ��
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

    // �÷��̾ �ٶ󺸴� �������κ��� _distance�Ÿ���ŭ ������ ������ ��ġ
    Vector3 CalcAttackStartPos(float _distance) => transform_.position + transform_.forward * _distance;

    // a�� b������ �Ÿ� ���
    float CalcDistance(Vector3 _a, Vector3 _b) => (_a - _b).magnitude;    

    void FanAttack()
    {
        Vector3 attackStartPos = CalcAttackStartPos(fanAttackDistance_);
        List<GameObject> enemies = GameObject.FindGameObjectsWithTag("Enemy").ToList();

        foreach (var enemy in enemies)
        {
            Transform enemyTransform = enemy.GetComponent<Transform>();
            float distance = CalcDistance(enemyTransform.position, attackStartPos);

            // Ÿ�������κ��� �Ÿ����� �־����.
            if (distance >= fanRadious_)
                continue;

            // Ÿ�������ؿ��� �ٶ󺸴� ������ �������� ���� �־����.
            Vector3 directionToEnmey = (enemyTransform.position - attackStartPos).normalized; // Ÿ���� ���ؿ��� ���� ��ġ�ϴ� ����
            float radianToEnemy = Vector3.Dot(transform_.forward, directionToEnmey); // ���� �ٶ󺸴� �����, Ÿ���� ���ؿ��� ���� ��ġ�ϴ� ������ �����Ͽ� ���� ������ ����Ѵ�.
            float degreeToEnemy = RadianToDegree(MathF.Acos(radianToEnemy));

            // ũ��� ���� �������� �ʴ� ������ ���� ��� �Լ��� ���ڷ� ������ ���͵��� ��� ���� ����(���̰� 1)�� �����̹Ƿ� �� ���� ũ���� ������ ������ �ʿ䰡 ����.
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

    // _target�� _positions�� �̷��� �ٰ��� ������ ������ �Ǵ��Ѵ�.
    bool IsCCW(Vector3 _target, params Vector3[] _positions)
    {
        int numPoints = _positions.Length;

        // �ٰ����� �� ���� �̿��Ͽ� ���� ���
        // ���� ��1, ��2, ��3, ��4�� ������ �簢���� ���
        // _target -> ��1 ���Ϳ�, ��1 -> ��2�� �������� up vector�� ���
        // _target -> ��2 ���Ϳ�, ��2 -> ��3�� �������� up vector�� ���
        // _target -> ��3 ���Ϳ�, ��3 -> ��4�� �������� up vector�� ���
        // _target -> ��4 ���Ϳ�, ��4 -> ��1�� �������� up vector�� ��츦 ��� �����Ѵٸ� �ٰ��� ���ο� ���� �����Ѵٴ� ���̴�.
        for (int i = 0; i < numPoints; i++)
        {
            Vector3 currentPoint = _positions[i];
            Vector3 nextPoint = _positions[(i + 1) % numPoints]; // ���� �� (������ �������� ù ��° ������ ��ȯ)

            Vector3 targetToCurrent = currentPoint - _target;   // target���� current ������ ����
            Vector3 currentToNext = nextPoint - currentPoint;   // current���� target ������ ����

            // targetToCurrent���Ϳ��� currentToNext�� ���ʿ� ���� �ʴ� ��� CCW(�ݽð�)�� �ƴ� CW�̹Ƿ� ���ο� ���� �ʴٰ� �Ǵ��Ѵ�.
            // 4���� �������� ��� ���� ��ȣ������ üũ�ص� �ȴ�.
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
        Vector3 rightDir = RotateDirectionVector(forwardDir, 90.0f);   // transform_.right�� ����...

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
            // �� �ڵ�� ������
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

            // Ÿ�������κ��� �Ÿ����� �־����.
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
