using BNG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class TestAgent3 : MonoBehaviour
{
    private List<Grabbable> _environmentGrabbables;      //�����еĿ�ץȡ����
    private Dictionary<Grabbable, bool> _environmentGrabbablesState;
    private Vector3[] _initialGrabbablePositions;   //��ץȡ����ĳ�ʼλ��
    private Quaternion[] _initialGrabbableRotations;//��ץȡ����ĳ�ʼ��ת
    private NavMeshAgent navMeshAgent;  // ���� NavMeshAgent

    public Transform itemRoot;
    public Grabbable nextGrabbable;     // ����Ŀ�ץȡ����
    public HandController handController;
    public float AreaDiameter = 7.5f;    // �����İ뾶��С����
    public float moveSpeed = 4f;


    private float[,] distanceMatrix; // �������
    private List<int> hamiltonianPath; // ���ܶ�·�����
    private int curGrabbableIndex = 0;

    private void ComputeDistanceMatrix()
    {
        int count = _environmentGrabbables.Count;
        distanceMatrix = new float[count + 1, count + 1];
        Vector3 agentStartPos = transform.position;
        for(int i = 0; i < count; i++)
        {
            Vector3 grabbablePos = _environmentGrabbables[i].transform.position;
            NavMeshPath agentToGrabbablePath = new NavMeshPath();
            float dist = agentToGrabbablePath.corners.Zip(agentToGrabbablePath.corners.Skip(1), Vector3.Distance).Sum();
            distanceMatrix[count, i] = dist;
            distanceMatrix[i, count] = dist;
        }

        for(int i = 0; i < count; i++)
        {
            for(int j = 0; j < count; j++)
            {
                if(i == j) continue;

                Vector3 start = _environmentGrabbables[i].transform.position;
                Vector3 end = _environmentGrabbables[j].transform.position;

                NavMeshPath path = new NavMeshPath();
                if(NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
                {
                    distanceMatrix[i, j] = path.corners.Zip(path.corners.Skip(1), Vector3.Distance).Sum();
                }
                else
                {
                    distanceMatrix[i, j] = float.MaxValue; // Set to an unreachable value if no path exists
                }
            }
        }
    }


    /// <summary>
    /// ���ݷ����TSP
    /// </summary>
    /// <returns></returns>
    private List<int> SolveTSP()
    {
        int n = _environmentGrabbables.Count;
        List<int> path = new List<int>();
        List<int> bestPath = new List<int>(); // �����洢���·��
        float bestDistance = float.MaxValue;  // �����洢���·���ľ���

        bool[] visited = new bool[n];  // ����Ƿ���ʹ�ĳ���ڵ�

        // �ݹ���ݺ���
        void Backtrack(int currentNode, float currentDistance, List<int> currentPath)
        {
            // ������нڵ㶼���ʹ��ˣ�����Ƿ������·��
            if(currentPath.Count == n)
            {
                if(currentDistance < bestDistance)
                {
                    bestDistance = currentDistance;
                    bestPath = new List<int>(currentPath);  // �������·��
                }
                return;
            }

            // �ݹ�ط���ÿһ��δ���ʵĽڵ�
            for(int i = 0; i < n; i++)
            {
                if(visited[i]) continue;

                // ���ʵ�ǰ�ڵ�
                visited[i] = true;
                currentPath.Add(i);
                float newDistance = currentDistance + distanceMatrix[currentNode, i];  // ���µ�ǰ·���ľ���

                // �ݹ�
                Backtrack(i, newDistance, currentPath);

                // ���ݣ�����ѡ��
                visited[i] = false;
                currentPath.RemoveAt(currentPath.Count - 1);
            }
        }

        // �ӳ�ʼ�ڵ㣨���������ʼλ�ã���ʼ��ִ�л���
        visited[0] = true;
        path.Add(0);  // ��ʼ·��������㣨�����λ�ã�
        Backtrack(0, 0, path);  // ����㿪ʼ����

        return bestPath;
    }


    private IEnumerator MoveToNextGrabbable()
    {
        GetNextGrabbable(out nextGrabbable);
        if(nextGrabbable != null)
        {
            navMeshAgent.SetDestination(nextGrabbable.transform.position);  // ����Ŀ��λ��Ϊ����Ŀ�ץȡ����
        }
        navMeshAgent.speed = moveSpeed;

        while(navMeshAgent.pathPending || navMeshAgent.remainingDistance > 0.5f)
        {
            yield return null;
        }

        // ����Ŀ��ص�
        Debug.Log("����Ŀ��λ��");

        handController.grabber.GrabGrabbable(nextGrabbable);
        _environmentGrabbablesState[nextGrabbable] = true;
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
            yield return null;
        }
        StartCoroutine(MoveToNextGrabbable());
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


        ComputeDistanceMatrix();
        hamiltonianPath = SolveTSP();
        curGrabbableIndex = 0;

    }

    /// <summary>
    /// ��ȡ����Ŀ�ץȡ����
    /// </summary>
    private void GetNextGrabbable(out Grabbable nextGrabbable)
    {
        nextGrabbable = _environmentGrabbables[curGrabbableIndex];
        curGrabbableIndex += 1;

    }

    private void Start()
    {
        navMeshAgent = GetComponent<NavMeshAgent>();  // ��ȡ NavMeshAgent ���

        GetEnvironmentGrabbables(out _environmentGrabbables, out _environmentGrabbablesState);

        StoreAllGrabbableObjects();   // ���泡���У���ץȡ����ĳ�ʼλ�ú���ת
        ResetAllGrabbableObjects();

        

        StartCoroutine(MoveToNextGrabbable());
    }


}