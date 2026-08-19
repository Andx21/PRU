using UnityEngine;

/// <summary>
/// Chạy hoạt ảnh bằng cách đổi sprite theo thời gian.
///
/// Dùng thay Animator cho con boss. Animator cần AnimatorController + AnimationClip
/// — dựng bằng code được nhưng rườm rà và dễ hỏng, trong khi việc cần làm chỉ là
/// lật qua 11 khung hình. Enemy đã null-safe với Animator nên bỏ hẳn cũng không sao,
/// và hiệu ứng chết là mờ dần bằng code chứ không phụ thuộc animation.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSheetAnimator : MonoBehaviour
{
    [Tooltip("Các khung hình, theo đúng thứ tự.")]
    [SerializeField] private Sprite[] frames;

    [Tooltip("Số khung mỗi giây.")]
    [SerializeField] private float fps = 10f;

    private SpriteRenderer sr;
    private float timer;
    private int index;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        // Pool tái dùng đối tượng: không reset thì con boss sau hiện ra giữa chừng
        // hoạt ảnh của con trước.
        timer = 0f;
        index = 0;
        Apply();
    }

    private void Update()
    {
        if (frames == null || frames.Length < 2 || fps <= 0f)
            return;

        timer += Time.deltaTime;
        float step = 1f / fps;

        if (timer < step)
            return;

        int advance = (int)(timer / step);
        timer -= advance * step;
        index = (index + advance) % frames.Length;
        Apply();
    }

    private void Apply()
    {
        if (sr != null && frames != null && frames.Length > 0)
        {
            sr.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }
    }
}
