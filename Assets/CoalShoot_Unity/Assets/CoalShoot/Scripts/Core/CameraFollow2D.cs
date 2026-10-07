using UnityEngine;

namespace CoalShoot
{
    /// <summary>
    /// Камера следит за одним или двумя игроками, смотрит вперёд по направлению взгляда
    /// и не выходит за границы уровня. Вешается на корневой объект рига камеры.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] float smoothTime = 0.15f;
        [Tooltip("Насколько камера смотрит вперёд, когда игрок один")]
        [SerializeField] float lookAhead = 2.6f;
        [SerializeField] float verticalOffset = 1f;
        [SerializeField] bool followY = false;
        [SerializeField] Vector2 minBounds = new Vector2(-1000f, -1000f);
        [SerializeField] Vector2 maxBounds = new Vector2(1000f, 1000f);

        Vector3 velocity;
        Camera cam;

        void Awake()
        {
            cam = GetComponentInChildren<Camera>();
        }

        public void SetBounds(Vector2 min, Vector2 max)
        {
            minBounds = min;
            maxBounds = max;
        }

        void LateUpdate()
        {
            var list = PlayerRegistry.Alive;
            if (list.Count == 0) return;

            Vector2 sum = Vector2.zero;
            for (int i = 0; i < list.Count; i++) sum += (Vector2)list[i].transform.position;
            Vector2 target = sum / list.Count;
            if (list.Count == 1) target.x += list[0].Motor.Facing * lookAhead;
            target.y += verticalOffset;

            Vector3 desired = new Vector3(target.x, followY ? target.y : transform.position.y, transform.position.z);
            if (cam != null && cam.orthographic)
            {
                float halfH = cam.orthographicSize;
                float halfW = halfH * cam.aspect;
                desired.x = Mathf.Clamp(desired.x, minBounds.x + halfW, Mathf.Max(minBounds.x + halfW, maxBounds.x - halfW));
                desired.y = Mathf.Clamp(desired.y, minBounds.y + halfH, Mathf.Max(minBounds.y + halfH, maxBounds.y - halfH)) /3;
            }
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
    }
}
