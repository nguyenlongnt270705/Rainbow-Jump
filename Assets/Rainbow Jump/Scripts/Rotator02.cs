using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Rotator02.cs: xoay vật thể ở một tốc độ khác trên axis z
namespace RainbowJump.Scripts
{

    public class Rotator02 : MonoBehaviour
    {
        public float speed = 3f;

        // Flow chính: mỗi frame quay object với speed lớn hơn khi chia cho 0.01
        // Update is called once per frame
        void Update()
        {
            transform.Rotate(0f, 0f, speed * Time.deltaTime / 0.01f, Space.Self);
        }
    }
}