using UnityEngine;

public enum MonsterRallyMovementMode
{
    Auto,
    GroundMotor,
    FlyingMotor,
    DirectTransform
}

[DisallowMultipleComponent]
public class MonsterSpawnRallyMover : MonoBehaviour
{
    private Vector3[] rallyPositions;
    private MonsterRallyMovementMode activeMovementMode;
    private float moveSpeed;
    private float arrivalDistance;
    private float waitAtEachPoint;
    private float waitRemaining;
    private float fixedZ;
    private int currentRallyIndex;

    private MonsterBase monsterAI;
    private MonsterHealth monsterHealth;
    private MonsterGroundMotor groundMotor;
    private MonsterFlyingMotor flyingMotor;
    private MonsterFacing monsterFacing;
    private CharacterController characterController;

    private bool restoreMonsterAI;
    private bool restoreCharacterController;
    private bool isRallyActive;

    // 지정된 랠리 경로 이동을 준비하고 기존 전투 AI를 일시 정지합니다
    public void BeginRally(
        Vector3[] positions,
        MonsterRallyMovementMode movementMode,
        float newMoveSpeed,
        float newArrivalDistance,
        float newWaitAtEachPoint,
        bool disableCombatAI,
        bool disableControllerDuringDirectMove)
    {
        CancelCurrentRally();

        if (positions == null || positions.Length <= 0)
        {
            Destroy(this);
            return;
        }

        rallyPositions = positions;
        moveSpeed = Mathf.Max(0.01f, newMoveSpeed);
        arrivalDistance = Mathf.Max(0.01f, newArrivalDistance);
        waitAtEachPoint = Mathf.Max(0f, newWaitAtEachPoint);
        waitRemaining = 0f;
        currentRallyIndex = 0;
        fixedZ = transform.position.z;

        CacheComponents();

        activeMovementMode =
            ResolveMovementMode(movementMode);

        PauseCombatAI(disableCombatAI);

        PrepareDirectMovement(
            disableControllerDuringDirectMove
        );

        isRallyActive = true;
    }

    // 랠리 포인트 대기와 이동 및 완료 여부를 매 프레임 처리합니다
    private void Update()
    {
        if (!isRallyActive)
        {
            return;
        }

        if (monsterHealth != null && monsterHealth.IsDead)
        {
            CompleteRally();
            return;
        }

        if (currentRallyIndex >= rallyPositions.Length)
        {
            CompleteRally();
            return;
        }

        if (waitRemaining > 0f)
        {
            waitRemaining -= Time.deltaTime;
            MaintainMovementWhileWaiting();
            return;
        }

        bool reached =
            MoveTowardCurrentRallyPoint();

        if (!reached)
        {
            return;
        }

        currentRallyIndex++;

        if (currentRallyIndex >= rallyPositions.Length)
        {
            CompleteRally();
            return;
        }

        waitRemaining = waitAtEachPoint;
    }

    // 랠리 이동에 사용할 몬스터 컴포넌트 참조를 가져옵니다
    private void CacheComponents()
    {
        monsterAI = GetComponent<MonsterBase>();
        monsterHealth = GetComponent<MonsterHealth>();
        groundMotor = GetComponent<MonsterGroundMotor>();
        flyingMotor = GetComponent<MonsterFlyingMotor>();
        monsterFacing = GetComponent<MonsterFacing>();
        characterController =
            GetComponent<CharacterController>();
    }

    // 몬스터가 보유한 이동 모터에 따라 실제 랠리 이동 모드를 결정합니다
    private MonsterRallyMovementMode ResolveMovementMode(
        MonsterRallyMovementMode requestedMode)
    {
        if (requestedMode ==
            MonsterRallyMovementMode.Auto)
        {
            if (flyingMotor != null)
            {
                return MonsterRallyMovementMode.FlyingMotor;
            }

            if (groundMotor != null)
            {
                return MonsterRallyMovementMode.GroundMotor;
            }

            return MonsterRallyMovementMode.DirectTransform;
        }

        if (requestedMode ==
                MonsterRallyMovementMode.GroundMotor &&
            groundMotor == null)
        {
            Debug.LogWarning(
                "Ground rally mode requires MonsterGroundMotor. Direct movement will be used.",
                this
            );

            return MonsterRallyMovementMode.DirectTransform;
        }

        if (requestedMode ==
                MonsterRallyMovementMode.FlyingMotor &&
            flyingMotor == null)
        {
            Debug.LogWarning(
                "Flying rally mode requires MonsterFlyingMotor. Direct movement will be used.",
                this
            );

            return MonsterRallyMovementMode.DirectTransform;
        }

        return requestedMode;
    }

    // 랠리 이동 중 기존 몬스터 전투 AI를 일시 정지합니다
    private void PauseCombatAI(bool disableCombatAI)
    {
        restoreMonsterAI = false;

        if (!disableCombatAI ||
            monsterAI == null ||
            !monsterAI.enabled)
        {
            return;
        }

        monsterAI.enabled = false;
        restoreMonsterAI = true;
    }

    // 직접 이동 모드에서 CharacterController를 임시로 비활성화합니다
    private void PrepareDirectMovement(
        bool disableControllerDuringDirectMove)
    {
        restoreCharacterController = false;

        if (activeMovementMode !=
                MonsterRallyMovementMode.DirectTransform ||
            !disableControllerDuringDirectMove ||
            characterController == null ||
            !characterController.enabled)
        {
            return;
        }

        characterController.enabled = false;
        restoreCharacterController = true;
    }

    // 현재 랠리 포인트를 향해 지정된 이동 방식으로 이동합니다
    private bool MoveTowardCurrentRallyPoint()
    {
        Vector3 targetPosition =
            rallyPositions[currentRallyIndex];

        targetPosition.z = fixedZ;

        UpdateFacingDirection(targetPosition);

        switch (activeMovementMode)
        {
            case MonsterRallyMovementMode.GroundMotor:
                return MoveUsingGroundMotor(
                    targetPosition
                );

            case MonsterRallyMovementMode.FlyingMotor:
                return MoveUsingFlyingMotor(
                    targetPosition
                );

            case MonsterRallyMovementMode.DirectTransform:
                return MoveUsingDirectTransform(
                    targetPosition
                );

            default:
                return false;
        }
    }

    // 지상 모터를 사용해 현재 랠리 포인트의 X 위치로 이동합니다
    private bool MoveUsingGroundMotor(
        Vector3 targetPosition)
    {
        if (groundMotor == null)
        {
            return true;
        }

        float horizontalDelta =
            targetPosition.x - transform.position.x;

        if (Mathf.Abs(horizontalDelta) <= arrivalDistance)
        {
            groundMotor.StopHorizontalMovement();
            groundMotor.TickMovement();
            return true;
        }

        float direction =
            Mathf.Sign(horizontalDelta);

        float frameDuration =
            Mathf.Max(Time.deltaTime, 0.0001f);

        float speedToAvoidOvershoot =
            Mathf.Abs(horizontalDelta) / frameDuration;

        float appliedSpeed =
            Mathf.Min(
                moveSpeed,
                speedToAvoidOvershoot
            );

        groundMotor.SetHorizontalVelocity(
            direction * appliedSpeed
        );

        groundMotor.TickMovement();

        return Mathf.Abs(
            targetPosition.x - transform.position.x
        ) <= arrivalDistance;
    }

    // 공중 모터를 사용해 현재 랠리 포인트로 이동합니다
    private bool MoveUsingFlyingMotor(
        Vector3 targetPosition)
    {
        if (flyingMotor == null)
        {
            return true;
        }

        return flyingMotor.MoveTowardPosition(
            targetPosition,
            moveSpeed,
            arrivalDistance,
            false
        );
    }

    // Transform을 직접 이동시켜 현재 랠리 포인트로 이동합니다
    private bool MoveUsingDirectTransform(
        Vector3 targetPosition)
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        Vector3 fixedPosition = transform.position;
        fixedPosition.z = fixedZ;
        transform.position = fixedPosition;

        return Vector3.Distance(
            transform.position,
            targetPosition
        ) <= arrivalDistance;
    }

    // 이동 방향의 X값을 기준으로 몬스터가 바라보는 방향을 갱신합니다
    private void UpdateFacingDirection(
        Vector3 targetPosition)
    {
        if (monsterFacing == null)
        {
            return;
        }

        float horizontalDelta =
            targetPosition.x - transform.position.x;

        if (Mathf.Abs(horizontalDelta) <= Mathf.Epsilon)
        {
            return;
        }

        monsterFacing.SetFacingDirection(
            horizontalDelta > 0f
        );
    }

    // 랠리 포인트 대기 중 지상 몬스터의 중력 처리를 유지합니다
    private void MaintainMovementWhileWaiting()
    {
        if (activeMovementMode !=
                MonsterRallyMovementMode.GroundMotor ||
            groundMotor == null)
        {
            return;
        }

        groundMotor.StopHorizontalMovement();
        groundMotor.TickMovement();
    }

    // 랠리 이동을 완료하고 원래 전투 AI와 충돌체 상태를 복구합니다
    private void CompleteRally()
    {
        if (!isRallyActive)
        {
            return;
        }

        if (groundMotor != null)
        {
            groundMotor.StopHorizontalMovement();
        }

        if (flyingMotor != null)
        {
            flyingMotor.ResetHomePosition();
        }

        Vector3 fixedPosition = transform.position;
        fixedPosition.z = fixedZ;
        transform.position = fixedPosition;

        isRallyActive = false;

        RestoreControlledComponents();

        Destroy(this);
    }

    // 기존 랠리가 진행 중이면 중단하고 제어 중이던 컴포넌트를 복구합니다
    private void CancelCurrentRally()
    {
        if (!isRallyActive)
        {
            return;
        }

        isRallyActive = false;

        if (groundMotor != null)
        {
            groundMotor.StopHorizontalMovement();
        }

        RestoreControlledComponents();
    }

    // 랠리 이동을 위해 비활성화했던 컴포넌트를 원래 상태로 되돌립니다
    private void RestoreControlledComponents()
    {
        if (restoreCharacterController &&
            characterController != null)
        {
            characterController.enabled = true;
        }

        if (restoreMonsterAI &&
            monsterAI != null)
        {
            monsterAI.enabled = true;
        }

        restoreCharacterController = false;
        restoreMonsterAI = false;
    }

    // 컴포넌트가 외부에서 제거될 경우 비활성화했던 상태를 복구합니다
    private void OnDestroy()
    {
        if (!isRallyActive)
        {
            return;
        }

        isRallyActive = false;
        RestoreControlledComponents();
    }
}