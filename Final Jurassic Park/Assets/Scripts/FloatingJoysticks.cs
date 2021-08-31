using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// let event by  keyboard, mouse, touch send to object
using UnityEngine.EventSystems;
using UnityEngine.UI;



public class FloatingJoysticks : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{

    // 인스펙터창에서도 수정해 주기위해
    [SerializeField]
    private RectTransform lever;
    private RectTransform rectTransform;

    [SerializeField]
    PlayerInput playerinput;


    // 조이스틱이 드래그 될 수 있는 범위를
    [SerializeField, Range(10, 150)]
    private float leverRange;

    //
    private Vector2 inputDirection;
    private bool isInput;

    //
    Vector2 JoystickPosition = Vector2.zero;

    private void Awake()
    {

        
        
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        rectTransform = GetComponent<RectTransform>();
        rectTransform.position = eventData.position;
        rectTransform.gameObject.SetActive(true);
        JoystickPosition = eventData.position;
        OnBeginDrag(eventData);

    }
    // when start drag 
    public void OnBeginDrag(PointerEventData eventData)
    {

        ControlJoystickLever(eventData);
        isInput = true;


    }

    // during dragging
    public void OnDrag(PointerEventData eventData)
    {

        ControlJoystickLever(eventData);

    }

    // as finish drag
    public void OnEndDrag(PointerEventData eventData)
    {
        
        lever.anchoredPosition = Vector2.zero;
        isInput = false;
        playerinput.Move(Vector2.zero);

    }


    private void ControlJoystickLever(PointerEventData eventData)
    {
        // 이벤트함수의 데이터를 통해서 포지션 가져올 수 있다.
        // 레버가 있어야 할 곳의 위치 = 터치된곳 - 조이스틱의 앵커 포지션 지점
        var inputPos = eventData.position - JoystickPosition;
        // 드래그한 조이스틱의 이동거리가 레버레인지보다 작으면 그대로 인풋위치를 사용
        // 레버레인지보다 크면 정규화된 벡터에 레버레인지를 곱한값을 사용 
        var inputVector = inputPos.magnitude < leverRange ? inputPos : inputPos.normalized * leverRange;
        lever.anchoredPosition = inputVector;

        //인풋벡터는 해상도를 기반으로 만들어진 값이므로 캐릭터의 이동속도로는 너무 큼
        // 입력받은 값을 0~1사이로 정규화된 값을 만들어 플레이어 인풋에 전달하기 위한 변
        inputDirection = inputVector / leverRange;

    }


    //캐릭터에게 이동벡터를 전달 

    private void InputControlVector()
    {
        playerinput.Move(inputDirection);


    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (isInput)
        {
            InputControlVector();

        }
    }
}