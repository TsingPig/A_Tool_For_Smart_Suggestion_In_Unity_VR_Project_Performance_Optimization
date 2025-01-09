using BNG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public abstract class BaseAgent : MonoBehaviour
{
    private int _curFinishCount = 0;
    private float _roundStartTIme = 0f;
    private Transform _itemRoot;

    protected Dictionary<Grabbable, bool> _environmentGrabbablesState;
    protected Vector3[] _initialGrabbablePositions;   //��ץȡ����ĳ�ʼλ��
    protected Quaternion[] _initialGrabbableRotations;//��ץȡ����ĳ�ʼ��ת
    protected NavMeshAgent _navMeshAgent;  // ���� NavMeshAgent
    protected SceneAnalyzer _sceneAnalyzer; // ����������



    public List<Grabbable> environmentGrabbables;      //�����еĿ�ץȡ����
    public bool drag = false;

    
    public Grabbable nextGrabbable;     // ����Ŀ�ץȡ����
    public HandController handController;
    public float AreaDiameter = 7.5f;    // �����İ뾶��С����
    public float moveSpeed = 6f;       // �ƶ��ٶ�
    public float twitchRange = 8f; // ����鴤�İ뾶��Χ
    public bool randomGrabble = false;


    protected IEnumerator MoveToNextGrabbable()
    {
        GetNextGrabbable(out nextGrabbable);

        if(nextGrabbable != null)
        {
            _navMeshAgent.SetDestination(nextGrabbable.transform.position);  // ����Ŀ��λ��Ϊ����Ŀ�ץȡ����
        }
        _navMeshAgent.speed = moveSpeed;

        float maxTimeout = 10f; // ��������ʱ�䣨�룩������������ʱ�仹û����Ŀ�꣬����Ϊ�ɹ�
        float startTime = Time.time;  // ��¼��ʼʱ��

        while(_navMeshAgent.pathPending || _navMeshAgent.remainingDistance > 0.5f)
        {
            // ����Ƿ�ʱ
            if(Time.time - startTime > maxTimeout)
            {
                Debug.LogWarning($"��ʱ��{GetType().Name} û����ָ��ʱ���ڵ���Ŀ��λ�ã�ǿ����Ϊ�ɹ�.");
                break;  // ��ʱ������ѭ������ΪĿ���ѵ���
            }

            yield return null;
        }

        // ����Ŀ��ص㣨��ʱ��Ϊ�ѵ��
        _environmentGrabbablesState[nextGrabbable] = true;

        if(drag)
        {
            handController.grabber.GrabGrabbable(nextGrabbable);
            StartCoroutine(Drag()); // ��ʼ��ק
        }
        else
        {
            if(_environmentGrabbablesState.Values.All(value => value)) // �������ֵ��Ϊ true
            {
                Debug.Log($"{GetType().Name}���{_curFinishCount}��, ����{(_roundStartTIme - Time.time):F2}��");
                ResetAllGrabbableObjects();
                _curFinishCount += 1;

                yield return null;
            }
            StartCoroutine(MoveToNextGrabbable());
        }
    }


    /// <summary>
    /// ����鴤
    /// </summary>
    protected IEnumerator RandomTwitch()
    {

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
        _navMeshAgent.SetDestination(randomPosition);
        _navMeshAgent.speed = moveSpeed * 0.6f; // �鴤ʱ�ٶȽ���
        Debug.Log("��ʼRandomTwitch");

        while(_navMeshAgent.pathPending || _navMeshAgent.remainingDistance > 0.6f)
        {
            yield return null;
        }

    }

    /// <summary>
    /// ��ק
    /// </summary>
    /// <param name="dragTime"></param>
    /// <returns></returns>
    protected IEnumerator Drag()
    {
        Debug.Log("��ʼDrag");

        yield return StartCoroutine(RandomTwitch());

        if(handController.grabber.HoldingItem)
        {
            handController.grabber.TryRelease();
        }

        Debug.Log("�ͷ�");

        if(_environmentGrabbablesState.Values.All(value => value)) // �������ֵ��Ϊ true
        {
            ResetAllGrabbableObjects();
            _curFinishCount += 1;
            yield return null;
        }
        StartCoroutine(MoveToNextGrabbable());
    }

    /// <summary>
    /// ��ȡ���������еĿ�ץȡ�����б�
    /// </summary>
    protected void GetEnvironmentGrabbables(out List<Grabbable> grabbables, out Dictionary<Grabbable, bool> grabbableState)
    {
        grabbables = new List<Grabbable>();
        grabbableState = new Dictionary<Grabbable, bool>();

        
        foreach(GameObject obj in _sceneAnalyzer.grabObjects)
        {
            var grabbable = obj.GetComponent<Grabbable>();
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
    protected void StoreAllGrabbableObjects()
    {
        _initialGrabbablePositions = new Vector3[environmentGrabbables.Count];
        _initialGrabbableRotations = new Quaternion[environmentGrabbables.Count];

        for(int i = 0; i < environmentGrabbables.Count; i++)
        {
            _initialGrabbablePositions[i] = environmentGrabbables[i].transform.position;
            _initialGrabbableRotations[i] = environmentGrabbables[i].transform.rotation;
        }
    }

    /// <summary>
    /// ���ü������п�ץȡ�����λ�ú���ת
    /// </summary>
    protected virtual void ResetAllGrabbableObjects()
    {
        _roundStartTIme = Time.time;
        for(int i = 0; i < environmentGrabbables.Count; i++)
        {
            _environmentGrabbablesState[environmentGrabbables[i]] = false;

            if(randomGrabble)
            {
                float randomX = Random.Range(-AreaDiameter, AreaDiameter);
                float randomZ = Random.Range(-AreaDiameter, AreaDiameter);
                float randomY = 2.5f;
                Vector3 newPosition = _itemRoot.position + new Vector3(randomX, randomY, randomZ);
                environmentGrabbables[i].transform.position = newPosition;
            }
            else
            {
                environmentGrabbables[i].transform.position = _initialGrabbablePositions[i];

            }
            Rigidbody rb = environmentGrabbables[i].GetComponent<Rigidbody>();
            if(rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }


    protected void Start()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();  // ��ȡ NavMeshAgent ���
        _sceneAnalyzer = GetComponent<SceneAnalyzer>();

        GetEnvironmentGrabbables(out environmentGrabbables, out _environmentGrabbablesState);

        StoreAllGrabbableObjects();   // ���泡���У���ץȡ����ĳ�ʼλ�ú���ת
        ResetAllGrabbableObjects();

        StartCoroutine(MoveToNextGrabbable());
    }

    protected abstract void GetNextGrabbable(out Grabbable nextGrabbbable);

}