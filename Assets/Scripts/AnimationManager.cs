using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    [SerializeField]private Animator animator;
    public float AnimationProgress { get; private set; }

    //싱글톤
    public static AnimationManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        animator = animator.GetComponent<Animator>();
    }

    private void Update()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        AnimationProgress = stateInfo.normalizedTime % 1f;
    }
}
