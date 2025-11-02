using UnityEngine;

public class Node : MonoBehaviour
{
    public bool isUsable; //공간을 물약으로 채울 수 있는지 여부를 결정합니다.

    public GameObject potion; //Node가 현재 가지고 있는 포션 오브젝트

    //생성자
    // public Node(bool isUsable, GameObject potion)
    // {
    //     this.isUsable = isUsable;
    //     this.potion = potion;
    // }

    // 추가: 셀 좌표
    public int x;
    public int y;

    public bool isEmpty => potion == null; // 변수명은 어디까지나 소문자로 시작할 것 아니면 걍 함수로 바꾸셈
    // public bool IsEmpty() => potion == null;
    
    // 칸 초기화(생성자 대신 사용)
    public void Initialize(int kX, int kY, bool kUsable = true) // 파라미터 이름 바꿔놈 앞에 접두사 붙여서 구분하셈 this 보다는
    {
        x = kX;
        y = kY;
        isUsable = kUsable;
    }

    // 칸에 포션 배치 + 좌표 동기화
    public void SetPotion(GameObject go)
    {
        potion = go;
        var p = go != null ? go.GetComponent<Potion>() : null;
        if (p != null)
        {
            p.SetIndices(x, y); // 기존 SetIndicies 호출 보완
        }
    }

    public void Clear() => potion = null;
}
