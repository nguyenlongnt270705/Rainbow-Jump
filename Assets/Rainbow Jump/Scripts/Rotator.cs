using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Rotator.cs: xoay vật thể theo axis z
namespace RainbowJump.Scripts
{
    public class Rotator : MonoBehaviour
    {

        public float rotatingSpeed = 100f;
        // Start is called before the first frame update

        // Flow chính: mỗi frame quay object với tốc độ rotatingSpeed
        // Update is called once per frame
        void Update()
        {
            transform.Rotate(0f, 0f, rotatingSpeed * Time.deltaTime);
        }
    }
}
