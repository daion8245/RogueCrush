using UnityEngine;

public class Node : MonoBehaviour
{
    public bool isUsable; //공간을 물약으로 채울 수 있는지 여부를 결정합니다.
    
    public GameObject potion; //Node가 현제 가지고 있는 포션 오브젝트

    //생성자
    public Node(bool isUsable, GameObject potion)
    {
        this.isUsable = isUsable;
        this.potion = potion;
    }
}
