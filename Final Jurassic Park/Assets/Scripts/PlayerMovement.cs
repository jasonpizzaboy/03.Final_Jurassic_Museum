using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController characterController;                    //플레이어 게임오브젝트를 움직일때 사용할 변수
    private PlayerInput playerInput;                                    //감지된 입력을 제공할 PlayerInput 컴포넌트에 래퍼런스를 할당할 변수
    private PlayerShooter playerShooter;
    private Animator animator;                                          //플레이어 애니매이션 통제 변수
    
    private Camera followCam;                                           //카메라의 방향을 기준으로 플레이어가 움직이는데, 카메라의 방향을 알기위해 메인카메라가 할당될 변수
    
    public float speed = 6f;                                            //움직임 속도
    public float jumpVelocity = 15f;                                    //점프순간 속도
    [Range(0.01f, 1f)] public float airControlPercent;                  //공중에 체류시 플레이어가 원래속도의 몇퍼센트를 통제할 것인지를 정하는 변수
                                                                        //Range()속성을 사용해서 0.01에서 1사이의 값으로 인스펙터 창에서 지정할수 있도록 만듬

    public float speedSmoothTime = 0.1f;                                //속도 스무싱의 지연시간
    public float turnSmoothTime = 0.1f;                                 //회전 스무싱의 지연시간
    
    private float speedSmoothVelocity;                                  //댐핑은 값의 연속적인 변화량을 기록하면서 이루어짐, 따라서 현재 값의 이동속도
    private float turnSmoothVelocity;                                   //현재 값의 회전속도
                                                                        //위의 두 변수는 댐핑에만 사용
    
    private float currentVelocityY;                                     //플레이어의 y방향 속도 저장할 변수
                                                                        //리지드바디 컴포넌트와 달리, 캐릭터 컨트롤러 컴포넌트는 자동으로 중력의 영향을 받지 않음
                                                                        //캐릭터 컨트롤러는 외부의 영향없이 내가 직접 속도 조정 가능한데 매 프래임마다
                                                                        //캐릭터 컨트롤러의 y방향 속도도 직접 통제해야함.
    
    public float currentSpeed =>
        new Vector2(characterController.velocity.x, characterController.velocity.z).magnitude;
                                                                        //수직방향의 속도(currentVelocityY)를 제외하고, x와 z 평면상에서의 속도를 나타내는 프로퍼티
                                                                        //지면상에서의 현재속도를 나타내는 프로퍼티
                                                                        //외부에서 플레이어가 어느정도 속도로 움직이는지 알려주는 프로퍼티
                                                                        //실제 출력되는 값은 characterController의 velocity를 통해 접근한 
                                                                        //x뱡향과 z방향의 속도를 벡터2로 만들어서 길이로 구한 값이 출력됨.

    private void Start()                                                //PlayerMovement cs가 사용할 컴포넌트에 대한 래퍼런스를 가져오는 함수
    {
        playerInput = GetComponent<PlayerInput>();                      //플레이어입력을 전달할 컴포넌트를 playerInput변수로 사용할수 있도록 할당
        playerShooter = GetComponent<PlayerShooter>();
        animator = GetComponent<Animator>();                            //움직임에 관한 입력에 따라 움직이는 애니매이션 재생하는 컴포넌트
        characterController = GetComponent<CharacterController>();      //실제 움직임을 적용할 컴포넌트
        followCam = Camera.main;                                        //플레이어가 정렬하는 방향의 기준이 될 플레이어 카메라를 가져옴.
    }

    private void FixedUpdate()                                          //물리갱신 주기에 맞춰서 자동 실행됨. 회전(Rotate), 움직임(Move)을 담당하는 매서드 실행
                                                                        //따라서 이동/회전에 관한 코드를 입력시 더욱 정확한 값으로 동작함.
    {
        if (currentSpeed > 0.2f || playerInput.fire || playerShooter.aimState == PlayerShooter.AimState.HipFire) Rotate();
                                                                        //현재 속도가 0.2보다 크거나(조금이라도 움직이거나) 발사버튼 누르면, 조준상태가 Hipfire 상태이면 
                                                                        //플레이어방향을 플레이어 카메라가 보고있는 방향으로 회전
                                                                        //currentSpeed값이 커지면 걸을때는 정렬하지 않고, 플레이어가 뛰는 순간부터 정렬함

        Move(playerInput.moveInput);                                    //감지된 사용자 입력을 넣어서 움직임 실행
        
        if (playerInput.jump) 
        Jump();                                   //점프입력 감지시 점프 실행

    }

    private void Update()                                               //매 프레임마다 실행됨. 물리적으로 정확한 수치를 요구하는 코드 입력시 오차발생 가능성이 있음.
    {
        UpdateAnimation(playerInput.moveInput);                         //유저입력에 맞춰 애니매이션 갱신
    }

    public void Move(Vector2 moveInput)                                 //playerInput으로부터 움직인 입력값을 전달받아서 실제로 움직이게 함
    {
        var targetSpeed = speed * moveInput.magnitude;                  //가고 싶은 속도 설정
                                                                        //magnitude 사용이유
                                                                        //게임패드의 스틱을 살짝 민 경우에는 입력값의 벡터길이가 1보다 작음 - 최댓값보다 작은 속도
        var moveDirection = Vector3.Normalize(transform.forward * moveInput.y + transform.right * moveInput.x);
                                                                        //이동하려는 방향을 벡터3로 만들어줌
                                                                        //현재 만들어준 moveDirection을 방향벡터로 사용할 것이고, 
                                                                        //현재 입력된 경우는 길이가 1이 아닌 경우가 발생할 수 있어서 Normalize로 정규화

        var smoothTime = characterController.isGrounded? speedSmoothTime: speedSmoothTime/airControlPercent;
                                                                        

        targetSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, smoothTime);  //SmoothDamp(원래값,원하는값,값의 현재변화량,지연시간 입력시 
                                                                                                         //원래값과 원하는 값을 부드럽게 이어주는 값을 생성.
                                                                            //현재속도에서 목표속도로 직전까지의 값의 변화량에 기반해서 지연시간을 적용해서 적절하게 이어진 값을 목표속도로 할당함
        
        currentVelocityY += Time.deltaTime * Physics.gravity.y;             //중력에 의해 바닥에 떨어지는 속도 설정-캐릭터컨트롤러 컴포넌트는 자동으로 떨어지지 않기 때문
                                                                            //Physics.gravity에는 중력가속도(-9.8)가 벡터3값으로 이미 할당됨
                                                                            //이중에서 y값을 가져와서 y방향의 가속도에 시간을 곱해서 현재 y방향의 속도를 갱신
                                                                            //가속도 * 시간간격 을 원래속도에 더해주면 얼마의 시간이 지났을때 속도가 얼마나 변한지 알수있음

        var velocity = moveDirection * targetSpeed + Vector3.up * currentVelocityY;
                                                                            //최종적으로 적용할 속도 계산
                                                                            //이동하려는 방향 * 원하는 속도 + (위쪽 벡터(0,1,0) * y방향 속도)
                                                                            //                           = (0,currentVelocity,0)

        characterController.Move(velocity * Time.deltaTime);

        if(characterController.isGrounded) currentVelocityY =0f;
    }

    public void Rotate()                                                //현재 플레이어캐릭터가 바라보는 방향을 카메라가 바라보고 있는 방향으로 전환
    {
        var targetRotation = followCam.transform.eulerAngles.y;

        targetRotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, turnSmoothTime);
                                                                            //targetRotation이 바로 적용되면 움직임이 딱딱해 보일수 있어서 스무딩 적용.
                                                                            //현재플레이어 위치의 오일러각의 y축 회전값을 입력함.
                                                                            //목표값, 값의 변화속도를 담당하는 변수를 할당, 지연시간 입력.
                                                                            //각도의 범위를 고려해서 댐핑이 일어남.
                                                                            //오일러각은 결과적으로는 같은 각을 표현하는데 표현이 두가지로 나올 수 있음.
                                                                            //따라서 스무스댐프앵글은 같은 각이 다르게 표현됨으로써 의도와 달리 더 많이 회전되는 것을 막아줌.



        transform.eulerAngles = Vector3.up * targetRotation;        //벡터3.y는 (0,1,0)이라서 y에 대해서만 targetRotation을 적용하는 셈
                                                                    //오일러각을 적용하면 내부적으로 자동으로 쿼터니언 회전으로 적용됨.

        


    }

    public void Jump()                                          //플레이어가 바닥에 닿아있는지를 체크해서 닿아있지 않으면 점프 못하게 함.
    {
        if(!characterController.isGrounded) 
        {
            playerInput.jump = false;
            return;             //플레이어가 바닥에 닿아있지 않으면 리턴으로 즉시 점프매서드 종료.
        }
        currentVelocityY = jumpVelocity;                        //바닥에 닿아있다면 y방향의 속도에 jumpVelocity를 할당해서 y방향의 속도를 재설정.
                                                                //매 fixedUpdate에서 Move()매서드가 실행될때 currentVelocityY값이 커지면서 위로 캐릭터가 점프함.
        
    }

    private void UpdateAnimation(Vector2 moveInput)             //사용자 입력을 받아서 사용자 입력에 맞춰서 현재 플레이어캐릭터의 애니매이션을 갱신 
                                                                //입력으로 moveInput값을 받아서 verticalMove/horizontalMove Parameter로 전달함
    {                                                           //verticalMove/horizontalMove Parameter는 각각 -1에서 1사이의 값으로 움직인 입력값을 전달받는 parameter
        var animationSpeedPercent = currentSpeed/ speed;        //현재 속도가 최고속도 대비 몇퍼센트인지 계산함.
        
        animator.SetFloat("Vertical Move", moveInput.y * animationSpeedPercent, 0.05f, Time.deltaTime);        
                                                                                        //수직방향 입력(moveInput.y)은 Vertical Move에 대입함.
        animator.SetFloat("Horizontal Move", moveInput.x* animationSpeedPercent, 0.05f, Time.deltaTime);      
                                                                                        //수평방향 입력(moveInput.x)은 Horizontal Move에 대입함.
                            //이렇게 하면 verticalMove/horizontalMove 값이 우리가 Set한 값으로 즉시 변화되지 않고, 이전값에서 Set한 값으로 부드럽게 변화함.
    }
}