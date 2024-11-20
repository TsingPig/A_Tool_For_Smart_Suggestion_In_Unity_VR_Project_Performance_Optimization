using UnityEngine;

/// <summary>
/// ���ص��ֲ���ײ���ϣ����ڼ���Ƿ���ײ����ץȡ���塣
/// </summary>
public class GrabberCollisionHandler : MonoBehaviour
{
    public VRAgent vrAgent;  // ���ø�����ű�


    private void OnTriggerEnter(Collider other)
    {
        vrAgent?.OnGrabberTriggerEnter(other);  
    }

    private void OnTriggerStay(Collider other)
    {
        vrAgent?.OnGrabberTriggerStay(other);
    }

    private void OnTriggerExit(Collider other)
    {
        vrAgent?.OnGrabberTriggerExit(other);  // ���ø����巽�����ݴ�����Ϣ
    }
}
