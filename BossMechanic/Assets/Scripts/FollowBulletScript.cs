using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowBulletScript : MonoBehaviour
{
    public float speed, lifeTime, damage = 16f;
    GameObject player;
    public GameObject damageParticles;
    public Animator anim;

    public GameObject bulletPrefab, smokeParticlesPrefab;
    private float angleIncrease;
    private float cir = Mathf.PI * 2;
    public float startRadius;
    public int bulletAmount;

    void Start()
    {
        StartCoroutine(explode(bulletAmount, 1, 0, false));
        player = GameObject.FindGameObjectWithTag("Player");
    }

    private void Update()
    {
        transform.position = Vector2.MoveTowards(transform.position, player.transform.position, Time.deltaTime * speed);
        var relativePos = player.transform.position - transform.position;
        var angle = Mathf.Atan2(relativePos.y, relativePos.x) * Mathf.Rad2Deg;
        var rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        transform.rotation = rotation;
    }

    IEnumerator explode(int bulletAmount, int reiterations, float sprayTimer, bool sprayOffset)
    {
        yield return new WaitForSeconds(lifeTime - 2.2f);
        anim.SetTrigger("CountDownTrigger");
        yield return new WaitForSeconds(2.2f);
        Instantiate(smokeParticlesPrefab, transform.position, Quaternion.identity);

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
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Instantiate(damageParticles, transform.position, Quaternion.identity);
        Destroy(gameObject, .01f);
    }
}
