using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Player.cs: điều khiển nhân vật chính, xử lý nhảy, kiểm tra game over, phát âm thanh
namespace RainbowJump.Scripts
{
    public class Player : MonoBehaviour
    {
        public float jumpForce = 10f;
        public Manager manager;

        public Rigidbody2D rb;

        // Flow chính:
        // 1) Cập nhật input (click/touch) -> nhảy và play tap sound
        // 2) Kiểm tra vị trí y < -5 -> game over + death sound
        // 3) OnTriggerEnter2D và OnBecameInvisible -> game over + death sound
        void Update()
        {
            // 1) Input nhảy
            if ((Input.GetMouseButtonDown(0) || Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) && !EventSystem.current.IsPointerOverGameObject())
            {
                // nếu click bên ngoài UI
                rb.velocity = Vector2.up * jumpForce;
                manager.PlayTapSound();
            }

            // 2) Rơi xuống dưới giới hạn y -> game over
            if (transform.position.y < -5f)
            {
                // báo game over
                manager.gameOver = true;
                manager.PlayDeathSound();
                // reset vị trí để tránh bị loop rơi
                transform.position = new Vector3(transform.position.x, -4.9f, transform.position.z);
            }
        }

        // 3) Va chạm obstacle -> game over
        void OnTriggerEnter2D(Collider2D col)
        {
            manager.gameOver = true;
            manager.PlayDeathSound();
        }

        // 4) Nếu ra khỏi camera => game over
        void OnBecameInvisible()
        {
            manager.gameOver = true;
            manager.PlayDeathSound();
        }
    }
}
