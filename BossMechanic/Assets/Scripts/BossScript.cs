using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class BossScript : MonoBehaviour
{
    public Vector2 startPos, roomSize;
    public float attackTimerMin, attackTimerMax;
    public GameObject player;
    private List<int> enabledAttacksList;
    private int[] enabledAttacks;
    public GameObject winScreen;

    [Header("Jump Attack")]
    public bool useRandomTarget;
    public float indicatorTimer, damageRadius, maxJumpDamage;
    public GameObject targetIndicatorPrefab, jumpingParticles, landingParticles;
    private Vector2 jumpTarget;
    [Tooltip("0 is do nothing, 1 is jump, and 2 is land")]
    public int inAir;
    public float inAirOffsetY, jumpSpeed, MOE; //MOE = Margin Of Error

    [Header("Camera Shake")]
    public CameraShake cameraShake;
    public float duration, magnitude;

    [Header("Circle Attack")]
    public GameObject bulletPrefab;
    private float angleIncrease;
    private float cir = Mathf.PI * 2;
    public float startRadius;
    public int bulletAmount;
    public LayerMask effectingLayers;

    [Header("Follow Bullet Attack")]
    public GameObject followBulletPrefab;

    [Header("Health")]
    public Image healthBarInfill;
    public float health, maxHealth;

    [Header("Enable Attacks")]
    public bool enableJumpAttack;
    public bool enableCircleAttack;
    public bool enableFollowBulletAttack;

    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        transform.position = startPos + new Vector2(0, inAirOffsetY);
        jumpTarget = startPos;
        inAir = 0;
        health = maxHealth;
        enabledAttacks = getEnabledAttacks().ToArray();
        Invoke("spawn", 1f);
    }

    private void Update()
    {
        #region JumpMovement
        //Flytter bossen op og ned, ud fra hvilket stadie hoppe er i.
        if (inAir == 1)
        {
            transform.position = Vector2.MoveTowards(transform.position, new Vector2(transform.position.x, inAirOffsetY), jumpSpeed * Time.deltaTime);
        }
        else if (inAir == 2)
        {
            transform.position = Vector2.MoveTowards(transform.position, jumpTarget, jumpSpeed * Time.deltaTime);
        }
        #endregion

        #region Health
        //Opdatere healthbaren
        healthBarInfill.fillAmount = health / maxHealth;

        if (health <= 0)
        {
            Time.timeScale = 0;
            winScreen.SetActive(true);
        }
        #endregion
    }

    void spawn()
    {
        inAir = 2;
        Instantiate(landingParticles, jumpTarget, Quaternion.identity);
        StartCoroutine(cameraShake.Shake(duration, magnitude));
        StartCoroutine(attackManager());
    }

    IEnumerator attackManager()
    {
        yield return new WaitForSeconds(Random.Range(attackTimerMin, attackTimerMax));

        int attackNr = getRandom(enabledAttacks);

        switch (attackNr)
        {
            case 1:
                StartCoroutine(jump());
                break;
            case 2:
                StartCoroutine(circleAttack(12, 3, 0.1f, true));
                break;
            case 3:
                StartCoroutine(bulletFollow(1, 0));
                break;
        }
    }

    List<int> getEnabledAttacks()
    {
        List<int> attacksEnabled = new List<int>();

        if (enableJumpAttack) { attacksEnabled.Add(1); }

        if (enableCircleAttack) { attacksEnabled.Add(2); }

        if (enableFollowBulletAttack) { attacksEnabled.Add(3); }

        return attacksEnabled;
    }

    int getRandom(int[] numbers)
    {
        int nr = Random.Range(0, numbers.Length);
        return numbers[nr];
    }

    IEnumerator jump()
    {
        jumpTarget = findTarget();
        ShowTarget();

        inAir = 1;
        Instantiate(jumpingParticles, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(indicatorTimer);

        inAir = 2;
        transform.position = new Vector2(jumpTarget.x, jumpTarget.y + inAirOffsetY);
        Instantiate(landingParticles, jumpTarget, Quaternion.identity);
        StartCoroutine(cameraShake.Shake(duration, magnitude));

        yield return new WaitForSeconds(.2f);
        checkVicinity(transform.position);
        StartCoroutine(attackManager());
    }

    void ShowTarget()
    {
        GameObject newIndicator = Instantiate(targetIndicatorPrefab, jumpTarget, Quaternion.identity);
        Destroy(newIndicator, indicatorTimer);
    }

    Vector2 findTarget()
    {
        Vector2 playerPos = player.transform.position;

        if (useRandomTarget)
        {
            float x = Random.Range(-roomSize.x, roomSize.x);
            float y = Random.Range(-roomSize.y, roomSize.y);

            return new Vector2(x, y);
        }
        else
        {
            float x = Mathf.Clamp(playerPos.x + Random.Range(-1f, 1f) * MOE, -roomSize.x + 1, roomSize.x - 1);
            float y = Mathf.Clamp(playerPos.y + Random.Range(-1f, 1f) * MOE, -roomSize.y + 1, roomSize.y - 1);

            return new Vector2(x, y);
        }
    }

    void checkVicinity(Vector2 origin)
    {
        Collider2D collider = Physics2D.OverlapCircle(origin, damageRadius, effectingLayers);
        if (collider && collider.gameObject.tag == "Player")
        {
            Vector2 dir = collider.gameObject.transform.position - transform.position;
            collider.gameObject.GetComponent<Rigidbody2D>().AddForce(dir * 7000 / Mathf.Pow(Vector2.Distance(transform.position, collider.gameObject.transform.position), 2));

            float damage = Mathf.Clamp(maxJumpDamage * (damageRadius - Vector2.Distance(transform.position, collider.gameObject.transform.position)) / damageRadius, 0, maxJumpDamage); ;

            player.GetComponent<PlayerController>().takeDamage(damage);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, damageRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, startRadius);
    }

    IEnumerator circleAttack(int bulletAmount, int reiterations, float sprayTimer, bool sprayOffset)
    {
        yield return new WaitForSeconds(.2f);
        for (int i = 0; i < reiterations; i++)
        {
            float angleOffset = 0;

            if (sprayOffset && i % 2 == 1)
            {
                angleOffset = 360f / bulletAmount / 2;
            }

            angleOffset = angleOffset * Mathf.Deg2Rad;
            angleIncrease = cir / bulletAmount;

            float x;
            float y;

            for (float angle = angleOffset; (angle + angleOffset) < cir - angleIncrease; angle += angleIncrease)
            {
                x = Mathf.Sin(angle) * startRadius + transform.position.x;
                y = Mathf.Cos(angle) * startRadius + transform.position.y;

                GameObject newBullet = Instantiate(bulletPrefab, new Vector2(x, y), Quaternion.identity);
                newBullet.transform.Rotate(new Vector3(0f, 0f, (120f - angle * Mathf.Rad2Deg) - 360f / bulletAmount));
            }

            yield return new WaitForSeconds(sprayTimer);
        }

        StartCoroutine(attackManager());
    }

    IEnumerator bulletFollow(int reiterations, float timer)
    {
        yield return new WaitForSeconds(.2f);
        for (int i = 0; i < reiterations; i++)
        {
            Vector2 clampedDifference = Vector2.ClampMagnitude(player.transform.position - transform.position, 1) + (Vector2)transform.position;
            Instantiate(followBulletPrefab, new Vector2(clampedDifference.x, clampedDifference.y), Quaternion.identity);
            yield return new WaitForSeconds(timer);
        }

        StartCoroutine(attackManager());
    }

    void takeDamage(float amount)
    {
        health -= amount;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 7 && collision.gameObject.GetComponent<BulletScript>())
        {
            takeDamage(collision.gameObject.GetComponent<BulletScript>().damage);
        }
    }
}
