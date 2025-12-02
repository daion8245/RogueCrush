using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    [SerializeField] private Animator animator;
    public float AnimationProgress { get; private set; }

    //싱글톤
    public static AnimationManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            animator = animator != null ? animator : GetComponent<Animator>();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Update()
    {
        if (!animator)
        {
            return;
        }
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        AnimationProgress = stateInfo.normalizedTime % 1f;
    }
}
