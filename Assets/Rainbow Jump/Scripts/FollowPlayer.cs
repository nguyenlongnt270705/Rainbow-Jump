using UnityEngine;

// FollowPlayer.cs: camera theo dõi player ở trục Y
namespace RainbowJump.Scripts
{
    public class FollowPlayer : MonoBehaviour
    {
        public Transform player;

        // Flow chính: nếu player cao hơn camera thì đẩy camera lên theo
        void Update()
        {
            // Flow: nếu player.y > camera.y thì dịch camera lên theo player
            if (player.position.y > transform.position.y)
            {
                transform.position = new Vector3(transform.position.x, player.position.y, transform.position.z);
            }
        }
    }
}
