using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Monster : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float walkSpeed = 1.5f;

    [Header("점프 설정")]
    [SerializeField] private float jumpHeight = 1.8f;
    [SerializeField] private float jumpTime = 0.4f;
    [SerializeField] private float detectionRadius = 2.0f; // 타워 감지 반경
    [SerializeField] private float jumpProbability = 0.3f; // 점프 확률 (0~1)
    [SerializeField] private float jumpCooldown = 1.0f;    // 점프 시도 사이의 대기 시간

    [SerializeField] private int zombiesPerRow = 5;
    [SerializeField] private float horizontalSpacing = 0.5f;

    // 상태 변수
    private bool isJumping = false;
    private bool isStacked = false;
    private float nextJumpCheckTime = 0f;

    // 컴포넌트 참조
    private Rigidbody2D rb;
    private Animator animator;
    private Collider2D monsterCollider;
    private Collider2D towerCollider;

    // 타워 관련 변수
    private static List<GameObject> towerMonsters = new List<GameObject>();
    private static float stackHeight = 0.8f;  // 각 좀비 사이의 높이

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if(rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.gravityScale = 1;
        rb.freezeRotation = true;

        animator = GetComponent<Animator>();
        monsterCollider = GetComponent<Collider2D>();

        towerCollider = GameObject.Find("Truck").GetComponent<Collider2D>();
    }

    void Update()
    {
        // 스택된 좀비는 더 이상 이동하지 않음
        if(isStacked)
        {
            return;
        }

        // 첫 번째 좀비인 경우 타워의 기초로 설정
        if(towerMonsters.Count == 0 && monsterCollider.IsTouching(towerCollider))
        {
            MakeBaseMonster();
        }

        // 걷기 동작 수행
        if(!isJumping)
        {
            // 타워 근처에서는 이동 속도를 서서히 줄여 멈추도록 함
            float distanceToTower = GetDistanceToTower();
            if(distanceToTower < detectionRadius)
            {
                float slowFactor = Mathf.Clamp(distanceToTower / detectionRadius, 0.3f, 1f); // 최소 속도 30% 보장
                transform.position += Vector3.left * (walkSpeed * slowFactor) * Time.deltaTime;
            }
            else
            {
                transform.position += Vector3.left * walkSpeed * Time.deltaTime;
            }

            // 랜덤 점프 시도 (점프 쿨타임 적용)
            if(Time.time > nextJumpCheckTime)
            {
                TryJump();
                nextJumpCheckTime = Time.time + jumpCooldown;
            }

            StabilizeTower();
        }
    }

    // 타워와의 거리 반환
    float GetDistanceToTower()
    {
        if(towerMonsters.Count == 0)
            return float.MaxValue;
        GameObject baseMonster = towerMonsters[0];
        return Vector2.Distance(transform.position, baseMonster.transform.position);
    }

    void TryJump()
    {
        if(towerMonsters.Count == 0 || isJumping || isStacked)
            return;

        float distanceToTower = GetDistanceToTower();

        // 타워 근처가 아닐 경우 점프하지 않음
        if(distanceToTower > detectionRadius * 1.2f)
            return;

        // 점프 확률로 시도
        if(Random.value <= jumpProbability)
        {
            StartCoroutine(JumpToTower());
            // 아직 리스트에 추가하지 않은 상태
        }
    }

    IEnumerator JumpToTower()
    {
        if(isJumping || isStacked)
            yield break;
        isJumping = true;

        Vector2 startPos = transform.position;

        // 타워 위치
        float anchorX = towerCollider.bounds.max.x;
        float anchorY = towerCollider.bounds.min.y;

        // (row, col) 계산
        int index = towerMonsters.Count;
        int row = index / zombiesPerRow;
        int col = index % zombiesPerRow;

        float targetX = anchorX + (col * horizontalSpacing);
        float targetY = anchorY + (row * stackHeight);

        // ---- 포물선 초기 속도 계산 ----
        float T = jumpTime;
        float gravity = 9.8f;

        // 원하는 추가 점프 높이 jumpHeight만큼 더 높이 갔다가 내려오기
        float dx = targetX - startPos.x;
        float dy = (targetY - startPos.y) + jumpHeight;

        // 뒤로 가는 것 방지
        if(dx > 0)
        {
            dx = -Mathf.Abs(dx);
        }

        // vx, vy 계산
        float vx = dx / T;
        // dy + (1/2)gT^2 = vy*T → vy = (dy + 0.5*g*T^2)/T
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
            float t = elapsed; // 실제 시간

            // x(t) = x0 + vx*t
            float newX = startPos.x + vx * t;
            // y(t) = y0 + vy*t - 0.5*g*t^2
            float newY = startPos.y + vy * t - 0.5f * gravity * (t * t);

            transform.position = new Vector3(newX, newY, 0);
            yield return null;
        }

        // 착지 후 위치 보정
        transform.position = new Vector3(targetX, targetY, 0);

        // 리스트 등록 등등
        if(!towerMonsters.Contains(gameObject))
            towerMonsters.Add(gameObject);

        rb.velocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        isJumping = false;
        isStacked = true;

        StabilizeTower();
    }




    void MakeBaseMonster()
    {
        isStacked = true;
        if(rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        // 첫 번째 좀비의 현재 y 좌표를 유지 (0으로 강제하지 않음)
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        transform.rotation = Quaternion.identity;

        if(!towerMonsters.Contains(gameObject))
        {
            towerMonsters.Add(gameObject);
        }
    }

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
            if(mScript == null || mScript.isJumping)
                continue; // 점프 중이면 무시

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

            // 점프 끝난 좀비만 위치 재정렬
            monster.transform.position = new Vector3(tX, tY, 0);
            monster.transform.rotation = Quaternion.identity;
        }
    }



    // 타워에서 제거될 때 호출
    void OnDestroy()
    {
        if(towerMonsters.Contains(gameObject))
        {
            towerMonsters.Remove(gameObject);
        }
    }

    // 디버그 시각화 (개발 중에 유용)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}