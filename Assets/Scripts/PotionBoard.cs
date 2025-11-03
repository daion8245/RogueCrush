using UnityEngine;
using Random = UnityEngine.Random;

public class PotionBoard : MonoBehaviour
{
    //보드의 크기 설정
    public int width = 6; //보드의 가로 크기
    public int height = 8;//보드의 세로 크기

    //노드간의 간격 설정
    public float spacingX; //노드간의 가로 간격
    public float spacingY; //노드간의 세로 간격

    public GameObject[] potionPrefab; //포션 프리팹 배열

    //포션 보드의 2차원 배열 선언
    private Node[,] _potionBoard; // 포션 보드의 2차원 배열
                                  // 2차원 배열이란? 2개의 인덱스를 사용하여 데이터를 저장하는 배열입니다. 예를 들어 아파트 생각하면 편합니다.
                                  // 각 층과 호수를 사용하여 특정 아파트를 지정할 수 있죠. 각 층은 첫 번째 인덱스, 각 호수는 두 번째 인덱스로 생각할 수 있습니다.

    public GameObject potionBoardGo; // 포션 보드 게임 오브젝트

    // "일단 강의에서 이렇게 만들라 해서 만들긴 하는데 이거 싱글톤 나중에 안쓸거같음
    public static PotionBoard Instance; // 싱글톤 인스턴스 
    // 싱글톤이란? 싱글톤 패턴은 클래스의 인스턴스가 오직 하나만 존재하도록 보장하는 디자인 패턴입니다.
    // 이를 통해 전역적으로 접근 가능한 단일 인스턴스를 제공할 수 있습니다. 보통 Instance라는 정적 변수를 사용하여 구현합니다. "우리 텍스트 RPG만들때 게임메니져 이걸로 만듬"

    [Header("Optional Parents")]
    public Transform potionsRoot; // 포션 전용 부모(선택)
   
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void Start()
    {
        InitializeBoard();
    }

    void InitializeBoard()
    {
        _potionBoard = new Node[width, height]; // 포션 보드 배열 초기화 "대충 안에 있는 노드들 다 지운다는거"

        spacingX = (float)(width - 1) / 2; // 가로 간격 설정
        spacingY = (float)(height - 1) / 2; // 세로 간격 설정

        // 포션 랜덤 생성 후 보드에 채우기
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 position = new Vector2(x - spacingX, y - spacingY); //포션의 위치 계산

                int randomIndex = Random.Range(0, potionPrefab.Length); //랜덤한 포션 프리팹 인덱스 선택

                // 포션 인스턴스 생성
                Transform parentForPotion = potionsRoot != null ? potionsRoot :
                                            (potionBoardGo != null ? potionBoardGo.transform : transform);
                GameObject potion = Instantiate(
                    potionPrefab[randomIndex],
                    position,
                    Quaternion.identity,
                    parentForPotion
                );// 포션 인스턴스 생성

                // (원본 호출 보완) 좌표 설정 메서드 철자 보정
                var pComp = potion.GetComponent<Potion>();
                if (pComp != null) pComp.SetIndices(x, y); //포션의 좌표 설정

                // 노드 생성 및 포션 보드에 할당
                // (기존) _potionBoard[x, y] = potion.AddComponent<Node>(); //노드 생성 및 포션 보드에 할당
                // → 노드는 '칸' GameObject로 분리해 영속 유지
                GameObject nodeGo = new GameObject($"Node_{x}_{y}");
                nodeGo.transform.SetParent(potionBoardGo != null ? potionBoardGo.transform : transform, false);
                nodeGo.transform.localPosition = position;

                Node node = nodeGo.AddComponent<Node>();
                node.Initialize(x, y);
                node.SetPotion(potion);

                _potionBoard[x, y] = node; //포션 보드에 노드 할당
            }
        }
    }
}
