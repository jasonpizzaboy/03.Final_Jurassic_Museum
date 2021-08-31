using UnityEngine;

public class PlayerHealth : LivingEntity                                //LivingEntity의 생명체로서의 기본기능을 가진채로 그위에 체력UI를 추가하고 
                                                                        //공격을 받았을때 피격효과, 파티클효과를 재생, 사망시에 사망효과음, 애니매이션 재생.
{
    private Animator animator;
    private AudioSource playerAudioPlayer;

    public AudioClip deathClip;
    public AudioClip hitClip;


    private void Awake()
    {
        playerAudioPlayer = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
    }

    protected override void OnEnable()                                  //PlayerHealth가 활성화될때 매번 실행될 매서드
    {
        base.OnEnable();                                                //LivingEntity의 OnEnable매서드를 실행하고 그 아래 필요한 처리를 삽입함
                                                                        //PlayerHealth 컴포넌트가 활성화될 때마다 체력상태를 리셋하고, 이후에 필요한 처리를 할 메서드
                                                                        //LiviingEntity를 확장하여 사용할 메서드
        UpdateUI();
    }
    
    public override void RestoreHealth(float newHealth)
    {
        base.RestoreHealth(newHealth);
        UpdateUI();
    }

    private void UpdateUI()                                             //플레이어의 체력UI 를 갱신하는 메서드
    {
        UIManager.Instance.UpdateHealthText(dead ? 0f : health);
    }
    
    public override bool ApplyDamage(DamageMessage damageMessage)
    {
        if (!base.ApplyDamage(damageMessage)) return false;

        EffectManager.Instance.PlayHitEffect(damageMessage.hitPoint, damageMessage.hitNormal, transform, EffectManager.EffectType.Flesh);
        playerAudioPlayer.PlayOneShot(hitClip);

        UpdateUI();
        
        return true;
    }
    
    public override void Die()
    {
        base.Die();

        playerAudioPlayer.PlayOneShot(deathClip);
        animator.SetTrigger("Die");

        UpdateUI();
    }
}