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
    private Vector3 _sceneCenter;

    protected Dictionary<Grabbable, bool> _environmentGrabbablesState;
    protected Vector3[] _initialGrabbablePositions;   //��ץȡ����ĳ�ʼλ��
    protected Quaternion[] _initialGrabbableRotations;//��ץȡ����ĳ�ʼ��ת
    protected NavMeshAgent _navMeshAgent;  // ���� NavMeshAgent
    protected SceneAnalyzer _sceneAnalyzer; // ����������



    public List<Grabbable> sceneGrabbables;      //�����еĿ�ץȡ����
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
                ResetSceneGrabbableObjects();
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
            ResetSceneGrabbableObjects();
            _curFinishCount += 1;
            yield return null;
        }
        StartCoroutine(MoveToNextGrabbable());
    }


    /// <summary>
    /// ��ȡ���������еĿ�ץȡ�����б�
    /// </summary>
    /// <param name="grabbables">��ץȡ�����б�</param>
    /// <param name="grabbableState">��ץȡ����״̬</param>
    protected void GetSceneGrabbables(out List<Grabbable> grabbables, out Dictionary<Grabbable, bool> grabbableState)
    {
        grabbables = new List<Grabbable>();
        grabbableState = new Dictionary<Grabbable, bool>();


        foreach(GameObject grabbableObject in _sceneAnalyzer.grabbableObjects)
        {
            var grabbable = grabbableObject.GetComponent<Grabbable>();
            grabbables.Add(grabbable);
            grabbableState.Add(grabbable, false);
        }
    }

    /// <summary>
    /// �洢���г����п�ץȡ����ı任��Ϣ
    /// </summary>
    protected void StoreSceneGrabbableObjects()
    {
        _initialGrabbablePositions = new Vector3[sceneGrabbables.Count];
        _initialGrabbableRotations = new Quaternion[sceneGrabbables.Count];

        for(int i = 0; i < sceneGrabbables.Count; i++)
        {
            _initialGrabbablePositions[i] = sceneGrabbables[i].transform.position;
            _initialGrabbableRotations[i] = sceneGrabbables[i].transform.rotation;
        }
    }

    /// <summary>
    /// ͨ����ȡNavMesh���������������񶥵����꣬����NavMesh�ļ�������
    /// </summary>
    /// <returns>NavMesh�Ľ�������</returns>
    private Vector3 GetNavMeshCenter()
    {
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        Vector3 center = Vector3.zero;
        int vertexCount = triangulation.vertices.Length;
        foreach(Vector3 vertex in triangulation.vertices)
        {
            center += vertex;
        }
        if(vertexCount > 0)
        {
            center /= vertexCount;
        }
        return center;
    }

    /// <summary>
    /// ���ü������п�ץȡ�����λ�ú���ת
    /// </summary>
    protected virtual void ResetSceneGrabbableObjects()
    {
        _roundStartTIme = Time.time;
        for(int i = 0; i < sceneGrabbables.Count; i++)
        {
            _environmentGrabbablesState[sceneGrabbables[i]] = false;

            if(randomGrabble)
            {
                float randomX = Random.Range(-AreaDiameter, AreaDiameter);
                float randomZ = Random.Range(-AreaDiameter, AreaDiameter);
                float randomY = 2.5f;
                Vector3 randomPosition = _sceneCenter + new Vector3(randomX, randomY, randomZ);
                NavMeshHit hit;
                if(NavMesh.SamplePosition(randomPosition, out hit, 5.0f, NavMesh.AllAreas))
                {
                    sceneGrabbables[i].transform.position = hit.position;
                }
                else
                {
                    Debug.LogWarning("���λ�ò���NavMesh�ϣ�δ�ҵ���Ч�Ŀɵ���λ�á�");
                }
            }
            else
            {
                sceneGrabbables[i].transform.position = _initialGrabbablePositions[i];
            }

            Rigidbody rb = sceneGrabbables[i].GetComponent<Rigidbody>();
            if(rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }



    protected void Start()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _sceneAnalyzer = GetComponent<SceneAnalyzer>();
        _sceneCenter = GetNavMeshCenter();

        GetSceneGrabbables(out sceneGrabbables, out _environmentGrabbablesState);

        StoreSceneGrabbableObjects();
        ResetSceneGrabbableObjects();

        StartCoroutine(MoveToNextGrabbable());
    }

    protected abstract void GetNextGrabbable(out Grabbable nextGrabbbable);

}