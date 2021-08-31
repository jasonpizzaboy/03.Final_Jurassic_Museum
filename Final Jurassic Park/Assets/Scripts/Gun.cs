using System;
using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public enum State                                                   //총의 상태를 나타내는 열거형
    {
        Ready,
        Empty,
        Reloading
    }
    public State state { get; private set; }                            //Gun 클래스 내부에서는 총의 상태를 변경가능하지만(private set), 
                                                                        //외부에서는 값을 가져오는 것만(get) 허용함.
    
    private PlayerShooter gunHolder;                                    //플레이어슈터의 컴포넌트의 레퍼런스가 gunHolder에 저장됨
                                                                        //총의 주인이 누구인지 알려주는 역할
    private LineRenderer bulletLineRenderer;                            //총알의 궤적을 그리기 위한 변수
    
    private AudioSource gunAudioPlayer;                                 //총알 발사/재장전 소리를 재생할 AudioSource컴포넌트를 가져와 할당할 변수
    public AudioClip shotClip;                                          //발사 소리
    public AudioClip reloadClip;                                        //재장전 소리
    
    public ParticleSystem muzzleFlashEffect;                            //파티클효과 변수
    public ParticleSystem shellEjectEffect;
    
    public Transform fireTransform;                                     //발사위치, 방향을 알려줄 변수
    public Transform leftHandMount;                                     //왼손의 위치를 알려줄 변수

    public float damage = 25;                                           //총의 데미지
    public float fireDistance = 100f;                                   //총알의 발사체크를 할 거리

    public int ammoRemain = 100;                                        //전체 남은 탄약수
    public int magAmmo;                                                 //현재 탄창에 있는 탄약수
    public int magCapacity = 30;                                        //탄창 용량

    public float timeBetFire = 0.12f;                                   //발사시간 간격
    public float reloadTime = 1.8f;                                     //재장전 시간
    
    [Range(0f, 10f)] public float maxSpread = 3f;                       //탄착군의 최대 범위, 커질수록 발사하는 총알이 흩어지는 범위가 넓어짐 
    [Range(1f, 10f)] public float stability = 1f;                       //반동안정성, 높으면 반동이 증가하는 속도가 낮아짐, 높아질수록 연사도중 반동이 누적되는 정도가 낮아짐
    [Range(0.01f, 3f)] public float restoreFromRecoilSpeed = 2f;        //연사를 중단한 다음, 탄퍼짐값이 0으로 되돌아오는데 까지의 속도를 결정
    private float currentSpread;                                        //현재 탄퍼짐의 정도
    private float currentSpreadVelocity;                                //현재 탄퍼짐 반경이 실시간으로 변하는 변화량을 기록
                                                                        //currentSpread에 smoothDamp를 사용해서 변경할때 사용

    private float lastFireTime;                                         //가장 마지막에 발사된 시점

    private LayerMask excludeTarget;                                    //총알이 쏘면 안되는 대상을 거르기 위한 레이어마스크



    private void Awake()                                            //필요한 컴포넌트를 가져옴
    {
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();

        bulletLineRenderer.positionCount = 2;                       //bulletLineRenderer에서 사용할 점의 갯수
                                                                    //(총구위치 -> 탄알이 닿은 위치)를 할당
        bulletLineRenderer.enabled = false;                         //총알 발사후 라인렌더러를 비활성화하지 못한 경우에 대비해서 한번더 코드작성
    }

    public void Setup(PlayerShooter gunHolder)                      //총의 주인인 플레이어슈터가 gun게임오브젝트의 gun컴포넌트를 대상으로 총의 초기화를 실행함
                                                                    //플레이어슈터가 실행할 것이기 때문에 public으로 접근범위를 허용함
                                                                    //gun을 쥐고있는 플레이어 슈터가 누구인지 알수있도록 초기화를 진행함
                                                                    //총의 입장에서 총을 쥐고있는 사람이 누구인지 알수있도록 하는 코드 입력
    {
        this.gunHolder = gunHolder;                                 //입력받은 gunHolder를 자기자신에 할당
        excludeTarget = gunHolder.excludeTarget;                    //총을 쏘면 안되는 타겟에 gunHolder를 가져와 할당함
                                                                    //총의 주인이 쏘지않기로 한 레이어를 가져와서 gun내부에 저장함
    }

    private void OnEnable()                                         //총이 활성화될 때마다 매번 총의 상태를 초기화하는 코드를 입력
    {
        magAmmo = magCapacity;                                      //현재 탄창의 총알수를 최대용량으로 초기화
        currentSpread = 0f;                                         //현재 탄퍼짐의 정도를 0으로 초기화
        lastFireTime = 0f;                                          //마지막 발사시점도 0으로 초기화
        state = State.Ready;                                        //현재 총의 상태를 '준비'상태로 설정  
    }

    private void OnDisable()                                        //건 컴포넌트가 비활성화될때마다 실행될 매서드
    {
        StopAllCoroutines();                                        //gun게임오브젝트 내부에 실행중인 코루틴이 있다면 모두 종료하는 처리
    }

    public bool Fire(Vector3 aimTarget)                             //gun 클래스 외부에서 총을 사용해서 발사를 시도하게 만드는 매서드
                                                                    //조준대상을 받아서 해당 방향으로 발사가 가능한 상태에서 Shot메서드를 실행함
                                                                    //결론적으로 Fire메서드는 Shot메서드를 안전하게 감싸는 역할
                                                                    //발사의 성공여부를 bool값으로 리턴함
    {
        if(state == State.Ready && Time.time >= lastFireTime + timeBetFire) //상태가 '준비'단계이고, 현재시간이 마지막 발사시점에서 발사간격이 지난 시점이면
        {
            var fireDirection = aimTarget - fireTransform.position;     //발사 방향과 거리 = 목표지점 - 발사지점
                                                                        //방향과 거리가 계산됨

            var xError = Utility.GetRandomNormalDistribution(0f, currentSpread);
            var yError = Utility.GetRandomNormalDistribution(0f, currentSpread);

            fireDirection = Quaternion.AngleAxis(yError, Vector3.up) * fireDirection;
            fireDirection = Quaternion.AngleAxis(xError, Vector3.up) * fireDirection;
    
            currentSpread += 1f/ stability;
            
            lastFireTime = Time.time;                                   //마지막 발사시점을 현재시점으로 갱신
            Shot(fireTransform.position, fireDirection);                //Shot(발사지점, 발사방향)

            return true;
        }


        return false;
    }
    
    private void Shot(Vector3 startPoint, Vector3 direction)        //실제 총알 발사처리가 이루어짐(총알발사지점, 방향을 입력받아서 실제 발사처리 실행)
    {
        RaycastHit hit;                                             //충돌정보를 hit에 저장함.
        Vector3 hitPosition;

        if(Physics.Raycast(startPoint, direction, out hit, fireDistance, ~excludeTarget))
        {
            var target = hit.collider.GetComponent<IDamageable>();

            if(target != null)
            {
                DamageMessage damageMessage;

                damageMessage.damager = gunHolder.gameObject;
                damageMessage.amount = damage;
                damageMessage.hitPoint = hit.point;
                damageMessage.hitNormal = hit.normal;

                target.ApplyDamage(damageMessage);
            }
            else
            {
                EffectManager.Instance.PlayHitEffect(hit.point, hit.normal, hit.transform);
            }

            hitPosition = hit.point;
        }
        else
        {
            hitPosition = startPoint + direction * fireDistance;
        }

        StartCoroutine(ShotEffect(hitPosition));

        magAmmo--;
        if(magAmmo <= 0) state = State.Empty;
    }

    private IEnumerator ShotEffect(Vector3 hitPosition)             //총알을 맞은 지점을 입력받아서 총알발사와 관련된 효과를 진행함
                                                                    //어느정도 시간을 들여서 과정이 진행되어서 코루틴을 사용함
    {
        muzzleFlashEffect.Play();
        shellEjectEffect.Play();

        gunAudioPlayer.PlayOneShot(shotClip);

        bulletLineRenderer.enabled = true;
        bulletLineRenderer.SetPosition(0, fireTransform.position);  //SetPosition(사용할 값의 순번, 해당 인덱스에 사용할 위치값)
                                                                    //SetPosition(총알의 시작점, 총구 끝 위치)
        bulletLineRenderer.SetPosition(1, hitPosition);             //SetPosition(총알의 끝점, 총알이 닿은 위치)

        yield return new WaitForSeconds(0.03f);                     //0.03초 대기하고(대기하는 동안 라인이 그려지면서 잔상처럼 보이게됨) 이후에 bulletLineRenderer가 비활성화 됨.

        bulletLineRenderer.enabled = false;
    }
    
    public bool Reload()                                            //외부에서 재장전을 시도하는 메서드, 성공유무에 따라 리턴값을 bool타입으로 반환함
                                                                    //외부에서 사용할 수 있도록 공개된 메서드, 아래의 ReloadRoutine을 안전하게 감싸는 역할
    {
        if(state == State.Reloading || ammoRemain <= 0 || magAmmo >= magCapacity)
        {
            return false;
        }

        StartCoroutine(ReloadRoutine());

        return true;
    }

    private IEnumerator ReloadRoutine()                             //실제 재장전은 여기서 이루어짐
    {
        state = State.Reloading;
        gunAudioPlayer.PlayOneShot(reloadClip);

        yield return new WaitForSeconds(reloadTime);

        var ammoToFill = Mathf.Clamp(magCapacity - magAmmo, 0, ammoRemain);

        magAmmo += ammoToFill;
        ammoRemain -= ammoToFill;

        state = State.Ready;
    }

    private void Update()                                           //현재 총알의 변동값을 상태에 따라서 갱신하는 역할
    {
        currentSpread = Mathf.Clamp(currentSpread, 0f, maxSpread);
        currentSpread = Mathf.SmoothDamp(currentSpread, 0f, ref currentSpreadVelocity, 1f/restoreFromRecoilSpeed);
    }
}