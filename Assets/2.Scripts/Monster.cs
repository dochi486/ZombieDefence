using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Monster : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float detectionRadius = 2.0f;  // 타워 감지 반경

    [Header("점프 설정")]
    [SerializeField] private float gravity = 9.8f;          // 중력 가속도 (포물선 공식용)
    [SerializeField] private float jumpTime = 0.4f;         // 점프 걸리는 시간
    [SerializeField] private float jumpHeight = 1.8f;       // 추가 점프 높이 (포물선에서 더 높이 뛰도록)
    [SerializeField] private float jumpProbability = 0.3f;  // 점프 확률 (0~1)
    [SerializeField] private float jumpCooldown = 1.0f;     // 점프 시도 쿨타임

    [Header("타워 쌓기 설정")]
    [SerializeField] private int zombiesPerRow = 5;         // X축에서 몇 마리까지 쌓고 다음 행으로 넘어갈지
    [SerializeField] private float horizontalSpacing = 0.5f;// X축 간격
    [SerializeField] private float stackHeight = 0.8f;      // Y축 간격

    // 상태 변수
    public bool isJumping = false;  // 점프 중인지 여부 (StabilizeTower에서 확인)
    private bool isStacked = false; // 스택에 완전히 들어갔는지
    private float nextJumpCheckTime = 0f;

    // 컴포넌트
    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D monsterCollider;
    private Collider2D towerCollider;

    // 전역(공유) 리스트
    private static List<GameObject> towerMonsters = new List<GameObject>();

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if(rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 1;
        rb.freezeRotation = true;

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        monsterCollider = GetComponent<Collider2D>();
        towerCollider = GameObject.Find("Truck").GetComponent<Collider2D>();
    }

    void Update()
    {
        // 이미 스택된 좀비는 이동/점프 안 함
        if(isStacked)
            return;

        // 첫 번째 좀비인 경우, 트럭과 닿으면 기초로 설정
        if(towerMonsters.Count == 0 && monsterCollider.IsTouching(towerCollider))
        {
            MakeBaseMonster();
        }

        // 점프 중이 아니면 이동 로직
        if(!isJumping)
        {
            float distanceToTower = GetDistanceToTower();

            // 타워 근처에서 감속
            if(distanceToTower < detectionRadius)
            {
                float slowFactor = Mathf.Clamp(distanceToTower / detectionRadius, 0.3f, 1f);
                transform.position += Vector3.left * (walkSpeed * slowFactor) * Time.deltaTime;
            }
            else
            {
                transform.position += Vector3.left * walkSpeed * Time.deltaTime;
            }

            // 랜덤 점프 시도 (쿨타임 적용)
            if(Time.time > nextJumpCheckTime)
            {
                TryJump();
                nextJumpCheckTime = Time.time + jumpCooldown;
            }
        }
    }

    float GetDistanceToTower()
    {
        if(towerMonsters.Count == 0)
            return float.MaxValue;
        GameObject baseMonster = towerMonsters[0];
        return Vector2.Distance(transform.position, baseMonster.transform.position);
    }

    // 랜덤 확률로 점프 시도
    void TryJump()
    {
        if(towerMonsters.Count == 0 || isJumping || isStacked)
            return;

        float distanceToTower = GetDistanceToTower();
        // 타워 근처가 아니면 점프 안 함
        if(distanceToTower > detectionRadius * 1.2f)
            return;

        // 점프 확률 체크
        if(Random.value <= jumpProbability)
        {
            StartCoroutine(JumpToTower());
        }
    }

    // 실제 포물선 점프 코루틴
    IEnumerator JumpToTower()
    {
        if(isJumping || isStacked)
            yield break;
        isJumping = true;

        Vector2 startPos = transform.position;

        // 트럭 경계
        float anchorX = towerCollider.bounds.max.x;
        float anchorY = towerCollider.bounds.min.y;

        // 이미 쌓인 좀비 개수로 (row, col) 계산
        int index = towerMonsters.Count;
        int row = index / zombiesPerRow;
        int col = index % zombiesPerRow;

        // 목표 위치
        float targetX = anchorX + (col * horizontalSpacing);
        float targetY = anchorY + (row * stackHeight);

        // 포물선 계산
        float T = jumpTime;       // 점프 걸리는 시간
        float dx = targetX - startPos.x;
        // dy에 jumpHeight만큼 더 높이 갔다 내려오도록
        float dy = (targetY - startPos.y) + jumpHeight;

        dx = -Mathf.Abs(dx);

        // 초기 속도 (물리 공식)
        // vx = dx / T
        float vx = dx / T;
        // vy = (dy + 0.5*g*T^2) / T
        float vy = (dy + 0.5f * gravity * (T * T)) / T;

        // 물리 초기화
        if(rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        float elapsed = 0f;
        while(elapsed < T)
        {
            elapsed += Time.deltaTime;
            float t = elapsed; // 실제 시간(0 ~ T)

            // x(t) = x0 + vx*t
            float newX = startPos.x + vx * t;
            // y(t) = y0 + vy*t - 0.5*g*t^2
            float newY = startPos.y + vy * t - 0.5f * gravity * (t * t);

            transform.position = new Vector3(newX, newY, 0);
            yield return null;
        }

        // 착지 후 보정
        transform.position = new Vector3(targetX, targetY, 0);

        // 스택 등록
        if(!towerMonsters.Contains(gameObject))
        {
            towerMonsters.Add(gameObject);
        }

        // 물리 멈춤
        if(rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        isJumping = false;
        isStacked = true;

        // 점프 끝난 후에만 타워 정렬
        StabilizeTower();
    }

    // 첫 번째 좀비를 기초로 설정
    void MakeBaseMonster()
    {
        isStacked = true;
        if(rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        transform.rotation = Quaternion.identity;

        if(!towerMonsters.Contains(gameObject))
        {
            towerMonsters.Add(gameObject);
        }

        // 첫 번째 좀비 설정 후 한 번만 StabilizeTower()
        StabilizeTower();
    }

    // 타워 정렬
    void StabilizeTower()
    {
        if(towerMonsters.Count == 0)
            return;

        float anchorX = towerCollider.bounds.max.x;
        float anchorY = towerCollider.bounds.min.y;

        for(int i = 0; i < towerMonsters.Count; i++)
        {
            GameObject monster = towerMonsters[i];
            if(monster == null)
                continue;

            Monster mScript = monster.GetComponent<Monster>();
            // 점프 중이거나 아직 스택 안 된 좀비는 건너뛴다
            if(mScript == null || mScript.isJumping || !mScript.isStacked)
                continue;

            // (row, col) 계산
            int row = i / zombiesPerRow;
            int col = i % zombiesPerRow;

            float tX = anchorX + (col * horizontalSpacing);
            float tY = anchorY + (row * stackHeight);

            Rigidbody2D r2d = monster.GetComponent<Rigidbody2D>();
            if(r2d != null)
            {
                r2d.velocity = Vector2.zero;
                r2d.angularVelocity = 0f;
                r2d.constraints = RigidbodyConstraints2D.FreezeAll;
            }

            monster.transform.position = new Vector3(tX, tY, 0);
            monster.transform.rotation = Quaternion.identity;
        }
    }

    void OnDestroy()
    {
        if(towerMonsters.Contains(gameObject))
        {
            towerMonsters.Remove(gameObject);
        }
    }
}
