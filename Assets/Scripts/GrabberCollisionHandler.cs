using UnityEngine;

public class GrabberCollisionHandler : MonoBehaviour
{
    public VRAgent vrAgent;  // ���ø�����ű�

    private void OnCollisionEnter(Collision collision)
    {
        vrAgent?.OnGrabberCollisionEnter(collision);  // ���ø����巽��������ײ��Ϣ
    }

    private void OnTriggerEnter(Collider other)
    {
        vrAgent?.OnGrabberTriggerEnter(other);  // ���ø����巽�����ݴ�����Ϣ
    }
}
