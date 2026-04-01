using UnityEngine;

// DestroyObstacle.cs: dọn obstacle khi nó xuống dưới vùng nhìn của camera
namespace RainbowJump.Scripts
{
    public class DestroyObstacle : MonoBehaviour
    {
        private Camera mainCamera;

        // Flow chính: lấy camera và mỗi frame kiểm tra, nếu obstacle vượt quá -7 so với camera thì destroy
        void Start()
        {
            // Get a reference to the MainCamera
            mainCamera = Camera.main;
        }

        void Update()
        {
            // Check if the gameobject's y position is less than the MainCamera's position -7
            if (transform.position.y < mainCamera.transform.position.y - 7.0f && gameObject.CompareTag("Obstacle"))
            {
                // Destroy the gameobject
                Destroy(gameObject);
            }
        }
    }
}