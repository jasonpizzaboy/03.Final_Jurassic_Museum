using UnityEngine;


public class PlayerShooter : MonoBehaviour                          //플레이어 입력에 따라 총을 쏘거나 재정전함, 플레이어 캐릭터의 왼손이 총의 왼손 위치에 붙어있도록 함.
{
    public enum AimState                                            //조준상태를 나타냄, 준비 또는 조준없이 발사하는 상태.
    {
        Idle,
        HipFire
    }

    public AimState aimState { get; private set; }                  //외부에서는 값을 가져가는 것만 가능, PlayerShooter class 내부에서만 값을 설정하는 것이 가능.

    public Gun gun;                                                 //사용할 Gun 게임오브젝트의 gun 컴포넌트
    
    public LayerMask excludeTarget;                                 //조준에서 제외할 대상을 거를 레이어마스크
    
    private PlayerInput playerInput;                                //플레이어 입력을 전달할 playerInput 컴포넌트
    private Animator playerAnimator;                                //플레이어 게임오브젝트의 애니매이터 컴포넌트를 가져와서 할당할 변수
    private Camera playerCamera;                                    //현재 메인카메라가 할당되어 있는 변수
    
    private float waitingTimeForReleasingAim = 2.5f;                //마지막 발사입력 시점에서 얼마간의 시간이 흐르면 다시 Idle상태로 돌아올지 대기시간을 정하는 변수
    private float lastFireInputTime;

    private Vector3 aimPoint;                                       //실제로 조준하고 있는 대상이 할당되어있는 변수
                                                                    //실제 총알이 맞을 것으로 예상되는 지점
    private bool linedUp => !(Mathf.Abs( playerCamera.transform.eulerAngles.y - transform.eulerAngles.y) > 1f);
                                                                    //플레이어가 바라보는 방향과 카메라가 바라보는 방향 사이의 각도가 벌어졌는지 bool값으로 리턴하는 프로퍼티
                                                                    //linedUp 프로퍼티는 카메라의 y축 회전과 플레이어 캐릭터의 y축 회전 사이의 절대값이 
                                                                    //1보다 큰경우는 false 반환, 1보다 작거나 같은 경우에는 true 반환함.

    private bool hasEnoughDistance => !Physics.Linecast(transform.position + Vector3.up * gun.fireTransform.position.y,gun.fireTransform.position, ~excludeTarget);
                                                                    //플레이어 캐릭터가 정면으로 총을 발사할 수 있을 정도의 넉넉한 공간을 확보하고 있는지를 반환하는 프로퍼티
                                                                    //플레이어 캐릭터의 발바닥의 위치에서 y축으로만 총의 위치만큼 높인 위치에서 총구의 위치 사이에 
                                                                    //어떤 콜라이더가 겹쳐있다면, 다른 물체에 총이 파묻혀있다는 말이 됨. 이때는 false가 반환됨.
                                                                    //반대로 겹치는 콜라이더가 존재하지 않으면 충분한 공간이 존재하는 것이라서 true를 반환함.

    void Awake()
    {
        if (excludeTarget != (excludeTarget | (1 << gameObject.layer)))
        {
            excludeTarget |= 1 << gameObject.layer;
        }
    }

    private void Start()
    {
        playerCamera = Camera.main;
        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        aimState = AimState.Idle;
        gun.gameObject.SetActive(true);
        gun.Setup(this);
    }

    private void OnDisable()
    {
        aimState = AimState.Idle;
        gun.gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (playerInput.fire)
        {
            lastFireInputTime = Time.time;
            Shoot();
        }
        else if (/*playerInput.reload */ gun.magAmmo == 0)
        {
            Reload();
        }
    }

    private void Update()
    {
        UpdateAimTarget();

        var angle = playerCamera.transform.eulerAngles.x;
        if(angle > 270f) angle -= 360f;

        angle = angle/ -180f + 0.5f;
        playerAnimator.SetFloat("Angle", angle);

        if(!playerInput.fire && Time.time >= lastFireInputTime + waitingTimeForReleasingAim)
        {
            aimState = AimState.Idle;
        }

        UpdateUI();
    }

    public void Shoot()
    {  
        if(aimState == AimState.Idle)
        {
            //if(linedUp) 
            aimState = AimState.HipFire;
        }
        else if(aimState == AimState.HipFire)
        {
            Debug.Log(hasEnoughDistance);
            if(hasEnoughDistance)
            {
                if(gun.Fire(aimPoint))
                {
                    playerAnimator.SetTrigger("Shoot");
                    playerInput.fire = false;
                }
            }
            else
            {
                aimState = AimState.Idle;
            }
        }
    }

    public void Reload()
    {
        if(gun.Reload())
        {
            playerAnimator.SetTrigger("Reload");
        }
    }

    private void UpdateAimTarget()
    {
        RaycastHit hit;

        var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if(Physics.Raycast(ray, out hit, gun.fireDistance, ~excludeTarget))
        {
            aimPoint = hit.point;

            if(Physics.Linecast(gun.fireTransform.position, hit.point, out hit, ~excludeTarget))
            {
                aimPoint = hit.point;
            }
        }
        else
        {
            aimPoint = playerCamera.transform.position + playerCamera.transform.forward * gun.fireDistance;
        }
    }

    private void UpdateUI()
    {
        if (gun == null || UIManager.Instance == null) return;
        
        UIManager.Instance.UpdateAmmoText(gun.magAmmo, gun.ammoRemain);
        
        UIManager.Instance.SetActiveCrosshair(hasEnoughDistance);
        UIManager.Instance.UpdateCrossHairPosition(aimPoint);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if(gun == null || gun.state == Gun.State.Reloading) return;

        playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);
        playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);

        playerAnimator.SetIKPosition(AvatarIKGoal.LeftHand, gun.leftHandMount.position);
        playerAnimator.SetIKRotation(AvatarIKGoal.LeftHand, gun.leftHandMount.rotation);

         
    }
}