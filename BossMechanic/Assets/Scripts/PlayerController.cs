using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Misc")]
    public Rigidbody2D rb;
    public float speed, startSpeed, dashSpeed, dashTime, dashCooldown, health, maxHealth;
    public State state;
    public Vector2 roomSize;
    public GameObject looseScreen;
    public ParticleSystem walkingParticles;

    [Header("Gun")]
    public int currentBulletCount;
    public int magazineSize;
    public float shotCooldown, reloadTime;
    public bool reloading;
    public Transform firePoint, gunfirePoint;
    public GameObject arm, bulletPrefab, gunFirePrefab;
    private Vector3 aim;

    private float timeSinceLastShot, reloadTimer;
    public TextMeshProUGUI bulletCount;
    public Image shootIndicator, vignette, healthBar;

    public enum State
    {
        walking,
        dashing
    }

    private void Start()
    {
        System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
        customCulture.NumberFormat.NumberDecimalSeparator = ".";

        System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

        health = maxHealth;

        speed = startSpeed;

        state = State.walking;

        currentBulletCount = magazineSize;
        timeSinceLastShot = shotCooldown;
    }

    void Update()
    {
        healthBar.fillAmount = health / maxHealth;
        vignette.color = new Color(1, 1, 1, (255.1f - 255 * (health / maxHealth)) / 255);


        if (health <= 0)
        {
            Debug.Log("Dead");
            Time.timeScale = 0;
            looseScreen.SetActive(true);
        }

        if (Input.GetAxis("Horizontal") == 0 || Input.GetAxis("Vertical") == 0 && walkingParticles.isEmitting)
        {
            walkingParticles.loop = false;
        }
        else if (!walkingParticles.isEmitting)
        {
            walkingParticles.loop = true;
            walkingParticles.Play();
        }

        float step = speed;
        rb.MovePosition(new Vector2(transform.position.x + Input.GetAxis("Horizontal") * step * Time.deltaTime, transform.position.y + Input.GetAxis("Vertical") * step * Time.deltaTime));
        transform.position = new Vector2(Mathf.Clamp(transform.position.x, -roomSize.x, roomSize.x), Mathf.Clamp(transform.position.y, -roomSize.y, roomSize.y));

        if (Input.GetKeyDown(KeyCode.Space) && state != State.dashing)
        {
            StartCoroutine(dash());
        }

        shootIndicator.fillAmount = 1 - timeSinceLastShot / shotCooldown;

        Vector3 mousePos = Input.mousePosition;
        mousePos += Camera.main.transform.forward * -10f;
        aim = Camera.main.ScreenToWorldPoint(mousePos);
        Rotate(arm, aim, 180);
        if (!reloading && timeSinceLastShot >= shotCooldown && Input.GetMouseButtonDown(0))
        {
            GameObject newBullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            Rotate(newBullet, aim, 0);
            gunfirePoint.gameObject.SetActive(false);
            gunfirePoint.gameObject.SetActive(true);
            currentBulletCount--;
            timeSinceLastShot = 0;
        }

        if (reloading || Input.GetKeyDown(KeyCode.R) && currentBulletCount < magazineSize || currentBulletCount <= 0)
        {
            if (!reloading)
            {
                reloadTimer = 0;
                reloading = true;
            }
            reload();
        }
        
        if (arm.transform.rotation.eulerAngles.z <= 0|| arm.transform.rotation.eulerAngles.z < 90)
        {
            arm.transform.localScale = new Vector3(1, 1, 1);
        }
        else if (arm.transform.rotation.eulerAngles.z <= 90 || arm.transform.rotation.eulerAngles.z < 180)
        {
            arm.transform.localScale = new Vector3(1, -1, 1);
        }
        else if (arm.transform.rotation.eulerAngles.z <= 180 || arm.transform.rotation.eulerAngles.z < 270)
        {
            arm.transform.localScale = new Vector3(1, -1, 1);
        }
        else if (arm.transform.rotation.eulerAngles.z <= 270 || arm.transform.rotation.eulerAngles.z < 360)
        {
            arm.transform.localScale = new Vector3(1, 1, 1);
        }

        if (!reloading)
        {
            bulletCount.SetText($"{currentBulletCount}/{magazineSize}");
        }

        timeSinceLastShot += Time.deltaTime;
        reloadTimer += Time.deltaTime;
    }

    IEnumerator dash()
    {
        state = State.dashing;
        speed = dashSpeed;
        GetComponent<Collider2D>().enabled = false;
        yield return new WaitForSeconds(dashTime);
        GetComponent<Collider2D>().enabled = true;
        speed = startSpeed;
        yield return new WaitForSeconds(dashCooldown);
        state = State.walking;
    }

    void reload()
    {
        if (reloadTimer >= reloadTime)
        {
            currentBulletCount = magazineSize;
            reloading = false;
        }
        bulletCount.SetText($"{(reloadTime - reloadTimer).ToString("F1")}");
    }

    void Rotate(GameObject objectToRotate, Vector3 objectToRotateTowards, float angleOffset)
    {
        var relativePos = objectToRotateTowards - objectToRotate.transform.position;
        var angle = Mathf.Atan2(relativePos.y, relativePos.x) * Mathf.Rad2Deg;
        var rotation = Quaternion.AngleAxis(angle + angleOffset, Vector3.forward);
        objectToRotate.transform.rotation = rotation;
    }

    public void takeDamage(float amount)
    {
        health -= amount;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == 8 && collision.gameObject.GetComponent<BulletScript>())
        {
            takeDamage(collision.gameObject.GetComponent<BulletScript>().damage);
        }
        else if (collision.gameObject.layer == 8 && collision.gameObject.GetComponent<FollowBulletScript>())
        {
            takeDamage(collision.gameObject.GetComponent<FollowBulletScript>().damage);
        }
    }
}
