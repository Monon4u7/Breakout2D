using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Paddle : MonoBehaviour
{
    #region Singleton

    static Paddle _instance;


    public static Paddle Instance => _instance;

    public bool PaddleIsTransforming { get; set; }


    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    #endregion
    Camera mainCamera;
    float paddleInitialY;
    //float _defaultPaddleWidthInPixels = 145;
    float _defaultPaddleWidthInPixels;
    //float _defaultLeftClamp = 310;
    float _defaultLeftClamp;
    //float _defaultRightClamp = 2250;
    float _defaultRightClamp;
    private SpriteRenderer sr;
    private BoxCollider2D boxCol;

    public bool PaddleIsShooting { get; set; }
    public GameObject leftMuzzle;
    public GameObject rightMuzzle;
    public Projectile bulletPrefab;

    public float extendShrinkDuration = 10;
    //public float paddleWidth = 2;
    public float paddleWidth;
    public float paddleHeight = 0.25f;


    private void Start()
    {
        _defaultLeftClamp = (float)(Camera.main.pixelRect.width * 0.1211);
        _defaultRightClamp = (float)(Camera.main.pixelRect.width * 0.8789);
        paddleWidth = (float)(Camera.main.pixelRect.width * 0.00078125);
        _defaultPaddleWidthInPixels = (float)(Camera.main.pixelRect.width * 0.0283203125);

        Cursor.visible = false;
        mainCamera = FindObjectOfType<Camera>();
        paddleInitialY = this.transform.position.y;
        sr = GetComponent<SpriteRenderer>();
        boxCol = GetComponent<BoxCollider2D>();
    }


    void Update()
    {
        PaddleMovement();
        UpdateMuzzlePosition();
    }

    private void UpdateMuzzlePosition()
    {
        leftMuzzle.transform.position = new Vector3(this.transform.position.x - (this.sr.size.x / 2) + 0.1f, this.transform.position.y + 0.1f, this.transform.position.z);
        rightMuzzle.transform.position = new Vector3(this.transform.position.x + (this.sr.size.x / 2) - 0.15f, this.transform.position.y + 0.1f, this.transform.position.z);
    }

    public void StartWidthAnimation(float newWidth)
    {
        StartCoroutine(AnimatePaddleWidth(newWidth));
    }

    private IEnumerator AnimatePaddleWidth(float width)
    {
        this.PaddleIsTransforming = true;
        this.StartCoroutine(ResetPaddleWidthAfterTime(this.extendShrinkDuration));

        if (width > this.sr.size.x)
        {
            float currentWidth = this.sr.size.x;

            
            while (currentWidth < width)
            {
                
                currentWidth += Time.deltaTime * 2;
                this.sr.size = new Vector2(currentWidth, paddleHeight);
                boxCol.size = new Vector2(currentWidth, paddleHeight);
                yield return null;
            }
        }
        else
        {
            float currentWidth = this.sr.size.x;
            
            while (currentWidth > width)
            {
                currentWidth -= Time.deltaTime * 2;
                this.sr.size = new Vector2(currentWidth, paddleHeight);
                boxCol.size = new Vector2(currentWidth, paddleHeight);
                yield return null;
            }
        }

        this.PaddleIsTransforming = false;
    }

    private IEnumerator ResetPaddleWidthAfterTime(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        this.StartWidthAnimation(this.paddleWidth);
    }

    void PaddleMovement()
    {
        float paddleShift = (_defaultPaddleWidthInPixels - ((_defaultPaddleWidthInPixels / 2) * this.sr.size.x));
        float leftClamp = _defaultLeftClamp - paddleShift;
        float rightClamp = _defaultRightClamp + paddleShift;
        float mousePositionPixels = Mathf.Clamp(Input.mousePosition.x, leftClamp, rightClamp);
        float mousePositionWorldX = mainCamera.ScreenToWorldPoint(new Vector3(mousePositionPixels, 0, 0)).x;
        this.transform.position = new Vector3(mousePositionWorldX, paddleInitialY, 0);
        //float mousePositionWorldX = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, 0, 0)).x;
        //this.transform.position = new Vector3(mousePositionWorldX, paddleInitialY, 0);
    }

    private void OnCollisionEnter2D(Collision2D coll)
    {
        if (coll.gameObject.tag == "Ball")
        {
            Rigidbody2D ballRb = coll.gameObject.GetComponent<Rigidbody2D>();
            Vector3 hitPoint = coll.contacts[0].point;
            Vector3 paddleCenter = new Vector3(this.gameObject.transform.position.x, this.gameObject.transform.position.y);

            ballRb.velocity = Vector2.zero;

            float difference = paddleCenter.x - hitPoint.x;

            if (hitPoint.x < paddleCenter.x)
            {
                ballRb.AddForce(new Vector2(-(Mathf.Abs(difference * 200)), BallsManager.Instance.initialBallSpeed));
            }
            else
            {
                ballRb.AddForce(new Vector2((Mathf.Abs(difference * 200)), BallsManager.Instance.initialBallSpeed));
            }
        }
    }

    public void StartShooting()
    {
        if (!this.PaddleIsShooting)
        {
            this.PaddleIsShooting = true;
            StartCoroutine(StartShootingRoutine());
        }
    }

    public IEnumerator StartShootingRoutine()
    {
        float fireCooldown = .5f;
        float fireCooldownLeft = 0;

        float shootingDuration = 10;
        float shootingDurationLeft = shootingDuration;

        //Debug.Log("Start shooting");

        while (shootingDurationLeft >= 0)
        {
            fireCooldownLeft -= Time.deltaTime;
            shootingDurationLeft -= Time.deltaTime;

            if (fireCooldownLeft <= 0)
            {
                this.Shoot();
                fireCooldownLeft = fireCooldown;
                //Debug.Log($"Shoot at {Time.time}");
            }

            yield return null;
        }

        //Debug.Log("Stop shooting");
        this.PaddleIsShooting = false;
        leftMuzzle.SetActive(false);
        rightMuzzle.SetActive(false);
    }

    private void Shoot()
    {
        leftMuzzle.SetActive(false);
        rightMuzzle.SetActive(false);

        leftMuzzle.SetActive(true);
        rightMuzzle.SetActive(true);

        AudioManager.Instance.effectPlayer.PlayOneShot(AudioManager.Instance.shot);
        this.SpawnBullet(leftMuzzle);
        this.SpawnBullet(rightMuzzle);
    }

    private void SpawnBullet(GameObject muzzle)
    {
        Vector3 spawnPosition = new Vector3(muzzle.transform.position.x, muzzle.transform.position.y + 0.2f, muzzle.transform.position.z);
        Projectile bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        bulletRb.AddForce(new Vector2(0, 450f));
    }
}
