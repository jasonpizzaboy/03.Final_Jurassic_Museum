public interface IDamageable
{
    bool ApplyDamage(DamageMessage damageMessage);              //bool으로 공격이 성공했는지 실패했는지 나타냄
                                                                //ApplyDamage매서드를 반드시 구현해야하는데, 해당 매서드는 DamageMessage타입의 오브젝트를 입력받음
                                                                //공격한 쪽에서 공격받은 쪽에게 전달하는 정보를 담은 구조체
                                                                //공격을 실행한 게임오브젝트, 공격의 양, 공격이 가해진 위치, 공격받은 표면의 노멀벡터가 포함됨.
}