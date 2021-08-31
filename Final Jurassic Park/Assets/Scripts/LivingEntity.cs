using System;
using UnityEngine;

public class LivingEntity : MonoBehaviour, IDamageable                      //게임속 생명채들이 가질 공통적인 속성을 구현해줌
                                                                            //체력과 체력을 회복하는 기능 + 공격을 받는 기능 + 죽을 수 있도록 함
                                                                            //LivingEntity를 상속하는 자식 클래스는 기존 메서드를 오버라이드해서 기존기능 + 자신만의 기능추가 가능
                                                                            //Idamageable class의 ApplyDamage 메서드를 반드시 구현해야함.
{
    public float startingHealth = 100f;                                     //기본체력
    public float health { get; protected set; }                             //현재 체력, 외부에서 체력을 읽을순 있지만, 덮어쓸수는 없음
                                                                            //protected set이기 때문에 LivingEntity 또는 이를 상속한 자식 클래스에서만 값을 변경할수 있음.
    public bool dead { get; protected set; }                                //사망상태를 표현
    
    public event Action OnDeath;                                            //액션타입 이벤트, 사망하는 순간에 실행될 콜백을 외부에서 접근하여 할당할수 있는 이벤트
    
    private const float minTimeBetDamaged = 0.1f;                           //공격과 공격받은 시간 사이의 최소 대기시간, 너무 짧은 간격으로 많은 공격이 들어오는 것을 방지하기 위한 목적
                                                                            //간격이 너무 크면 정상적인 공격도 막히는 경우가 발생하기 때문에 짧은 것이 자연스러움
                                                                            //게임도중에 변경되는 경우가 없기 때문에 상수(const)로 선언함
    private float lastDamagedTime;                                          //최근에 공격당한 시점

    protected bool IsInvulnerabe                                            //현재 livingEntity가 무적모드인지 반환하는 프로퍼티
                                                                            //protected라서 내부에서만 사용함
    {
        get
        {
            if (Time.time >= lastDamagedTime + minTimeBetDamaged) return false; //현재시간이 마지막으로 공격당한 시간에서 최고 공격대기시간이상 지났으면 공격을 당할수 있는 상태

            return true;                                                        //지나지 않았으면 공격을 당할수 없는(무적모드) 상태
        }
    }
    
    protected virtual void OnEnable()                                           //LivingEntity의 자식 클래스에서 오버라이드 가능함
                                                                                //virtual 키워드로 LivingEntity의 기존기능을 사용하되 자신의 기능을 적용하여 확장가능
                                                                                //가장 먼저 실행되고, 생명체를 리셋함
    {
        dead = false;                                                           //죽은 것을 거짓으로 처리하고
        health = startingHealth;                                                //체력을 시작체력으로 갱신함.
    }

    public virtual bool ApplyDamage(DamageMessage damageMessage)                //외부에서 LivingEntity를 공격하는데 사용됨
    {
        if (IsInvulnerabe || damageMessage.damager == gameObject || dead) return false;
                                                                            //무적상태이거나, 데미지메시지에 데미지를 가한 사람이 자기자신이거나, 사망상태이면 공격당하는 것이 불가능

        lastDamagedTime = Time.time;                                            //최근공격시점을 현재시간으로 갱신
        health -= damageMessage.amount;                                         //체력에서 데미지 양만큼 차감
        
        if (health <= 0) Die();                                                 //체력이 0이하이면 Die()메서드를 실행해서 사망처리

        return true;                                                            //ApplyDamage 메서드를 종료하고, 공격을 한 대상에게 공격이 성공적으로 들어갔다는 신호를 보냄
    }
    
    public virtual void RestoreHealth(float newHealth)                          //자기자신의 체력을 회복하는 메서드, virtual이라서 자식클래스에서 오버라이드 가능
    {
        if (dead) return;                                                       //사망상태이면 해당 메서드를 종료
        
        health += newHealth;                                                    //그게 아니면 체력을 입력받은 값만큼 회복함
    }
    
    public virtual void Die()                                                   //LivingEntity의 사망을 구현하는 메서드
    {
        if (OnDeath != null) OnDeath();                                         //OnDeath 이벤트에 최소 하나이상의 이벤트 리스너가 등록되어있다면, 해당 이벤트를 실행함
        
        dead = true;                                                            //현재 LivingEntity를 사망상태로 변경함.
    }
}