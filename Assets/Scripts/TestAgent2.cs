using BNG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class TestAgent2 : MonoBehaviour
{
    private List<Grabbable> _environmentGrabbables;      //�����еĿ�ץȡ����
    private Dictionary<Grabbable, bool> _environmentGrabbablesState;
    private Vector3[] _initialGrabbablePositions;   //��ץȡ����ĳ�ʼλ��
    private Quaternion[] _initialGrabbableRotations;//��ץȡ����ĳ�ʼ��ת
    private NavMeshAgent navMeshAgent;  // ���� NavMeshAgent
    private bool dragging = false;

    public Transform itemRoot;
    public Grabbable neareastGrabbable;     // ����Ŀ�ץȡ����
    public HandController handController;
    public float AreaDiameter = 7.5f;    // �����İ뾶��С����
    public float moveSpeed = 4f;

    private IEnumerator MoveToNearestGrabbable()
    {
        if(neareastGrabbable != null)
        {
            navMeshAgent.SetDestination(neareastGrabbable.transform.position);  // ����Ŀ��λ��Ϊ����Ŀ�ץȡ����
        }
        navMeshAgent.speed = moveSpeed;

        while(navMeshAgent.pathPending || navMeshAgent.remainingDistance > 0.5f)
        {
            yield return null;
        }

        // ����Ŀ��ص�
        Debug.Log("����Ŀ��λ��");
        handController.grabber.GrabGrabbable(neareastGrabbable);
        _environmentGrabbablesState[neareastGrabbable] = true;
        StartCoroutine(Drag()); // ��ʼ��ק
    }

    /// <summary>
    /// ����鴤
    /// </summary>
    private IEnumerator RandomTwitch()
    {
        // ��������鴤�ķ�Χ
        float twitchRange = 8f; // ����鴤�İ뾶��Χ
        float randomOffsetX = Random.Range(twitchRange / 2, twitchRange); // X��������ƫ��
        float randomOffsetZ = Random.Range(twitchRange / 2, twitchRange); // Z��������ƫ��
        randomOffsetX = Random.Range(-1, 1) >= 0 ? randomOffsetX : -randomOffsetX;
        randomOffsetZ = Random.Range(-1, 1) >= 0 ? randomOffsetZ : -randomOffsetZ;
        // ������λ�ã��ڵ�ǰλ�õĸ�������鴤��
        Vector3 randomPosition = transform.position + new Vector3(randomOffsetX, 0, randomOffsetZ);

        // �����ת��ģ��鴤ʱ�����תȦ��
        float randomRotationY = Random.Range(-30f, 30f); // �� -30 �� 30 �ȷ�Χ����ת
        transform.Rotate(0, randomRotationY, 0);

        // ����Ŀ��λ�ã����ʹ�� NavMeshAgent��
        navMeshAgent.SetDestination(randomPosition);
        navMeshAgent.speed = moveSpeed * 0.6f; // �鴤ʱ�ٶȽ���
        Debug.Log("��ʼRandomTwitch");

        while(navMeshAgent.pathPending || navMeshAgent.remainingDistance > 0.6f)
        {
            yield return null;
        }

    }

    /// <summary>
    /// ��ק
    /// </summary>
    /// <param name="dragTime"></param>
    /// <returns></returns>
    private IEnumerator Drag()
    {
        dragging = true;
        Debug.Log("��ʼDrag");

        yield return StartCoroutine(RandomTwitch());

        if(handController.grabber.HoldingItem)
        {
            handController.grabber.TryRelease();
        }
        dragging = false;

        Debug.Log("�ͷ�");

        if(_environmentGrabbablesState.Values.All(value => value)) // �������ֵ��Ϊ true
        {
            ResetAllGrabbableObjects();
            yield return null;
        }
        StartCoroutine(MoveToNearestGrabbable());
    }

    /// <summary>
    /// ��ȡ���������еĿ�ץȡ�����б�
    /// </summary>
    private void GetEnvironmentGrabbables(out List<Grabbable> grabbables, out Dictionary<Grabbable, bool> grabbableState)
    {
        grabbables = new List<Grabbable>();
        grabbableState = new Dictionary<Grabbable, bool>();
        foreach(Transform child in itemRoot.transform)
        {
            var grabbable = child.GetComponent<Grabbable>();
            if(grabbable)
            {
                grabbables.Add(grabbable);
                grabbableState.Add(grabbable, false);
            }
        }
    }

    /// <summary>
    /// �洢���г����п�ץȡ����ı任��Ϣ
    /// </summary>
    private void StoreAllGrabbableObjects()
    {
        _initialGrabbablePositions = new Vector3[_environmentGrabbables.Count];
        _initialGrabbableRotations = new Quaternion[_environmentGrabbables.Count];

        for(int i = 0; i < _environmentGrabbables.Count; i++)
        {
            _initialGrabbablePositions[i] = _environmentGrabbables[i].transform.position;
            _initialGrabbableRotations[i] = _environmentGrabbables[i].transform.rotation;
        }
    }

    /// <summary>
    /// ���ü������п�ץȡ�����λ�ú���ת
    /// </summary>
    private void ResetAllGrabbableObjects()
    {
        for(int i = 0; i < _environmentGrabbables.Count; i++)
        {
            _environmentGrabbablesState[_environmentGrabbables[i]] = false;

            float randomX = Random.Range(-AreaDiameter, AreaDiameter);
            float randomZ = Random.Range(-AreaDiameter, AreaDiameter);
            float randomY = 2.5f;
            Vector3 newPosition = itemRoot.position + new Vector3(randomX, randomY, randomZ);
            _environmentGrabbables[i].transform.position = newPosition;
            _environmentGrabbables[i].transform.rotation = _initialGrabbableRotations[i];

            Rigidbody rb = _environmentGrabbables[i].GetComponent<Rigidbody>();
            if(rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// ��ȡ����Ŀ�ץȡ����
    /// </summary>
    private void GetNearestGrabbable(out Grabbable nearestGrabbable)
    {
        nearestGrabbable = _environmentGrabbables
            .Where(grabbable => _environmentGrabbablesState[grabbable] == false)
            .OrderBy(grabbable => Vector3.Distance(transform.position, grabbable.transform.position))
            .FirstOrDefault();
    }

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();  // ��ȡ NavMeshAgent ���

        GetEnvironmentGrabbables(out _environmentGrabbables, out _environmentGrabbablesState);
        GetNearestGrabbable(out neareastGrabbable);

        StoreAllGrabbableObjects();   // ���泡���У���ץȡ����ĳ�ʼλ�ú���ת

        StartCoroutine(MoveToNearestGrabbable());
    }

    private void Update()
    {
        GetNearestGrabbable(out neareastGrabbable);
    }
}