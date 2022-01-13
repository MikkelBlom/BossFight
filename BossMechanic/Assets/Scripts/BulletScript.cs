using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public float speed, lifeTime, damage;
    public GameObject damageParticles;
    Rigidbody2D rb;
     
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.AddForce(transform.right * speed);
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Instantiate(damageParticles, transform.position, Quaternion.identity);
        Destroy(gameObject, .01f);
    }
}
