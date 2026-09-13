using UnityEngine;

namespace MintLandDemo.Controller.NPC
{
    public class NpcLookAtPlayer : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private Transform player;
        [SerializeField] private float detectRadius = 3f;

        [Header("Rotation")]
        [SerializeField] private float rotateSpeed = 5f;

        private Quaternion _originalRotation;

        private void Start()
        {
            // 如果没在 Inspector 里指定，就用 Tag 找
            if (player == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }

            _originalRotation = transform.rotation;
        }

        private void Update()
        {
            if (player == null) return;

            float dist = Vector3.Distance(transform.position, player.position);
            Quaternion targetRot = _originalRotation;

            if (dist <= detectRadius)
            {
                Vector3 dir = player.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    targetRot = Quaternion.LookRotation(dir);
            }

            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }
    }
}