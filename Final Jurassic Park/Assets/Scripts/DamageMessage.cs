using UnityEngine;

public struct DamageMessage                     //DamageMessage를 클래스가 아닌 구조체로 구성한 이유는 클래스는 래퍼런스 타입이기 때문에 메시지를 전달받은 측에서
                                                //데미지 메시지를 임의로 수정하면 같은 메시지를 받은 다른 곳에서도 해당 변경사항이 변경되기 때문임.
                                                //구조체는 value타입이기 때문에 데미지 메시지를 전달받은 쪽에서 필요에 따라 마음대로 수정해도 다른곳에 영향을 주지 않음.
{
    public GameObject damager;                  //공격한 대상
    public float amount;                        //공격의 양

    public Vector3 hitPoint;                    //공격한 위치
    public Vector3 hitNormal;                   //공격한 위치의 노멀벡터
}