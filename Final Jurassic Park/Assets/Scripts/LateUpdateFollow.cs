using UnityEngine;

public class LateUpdateFollow : MonoBehaviour
{
    public Transform targetToFollow;                            //따라갈 대상의 위치를 변수로 선언함

    private void LateUpdate()                                   //현재 위치와 회전을 타겟의 위치와 회전으로 갱신함.
    {
        transform.position = targetToFollow.position;
        transform.rotation = targetToFollow.rotation;
    }
}