using BNG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;


public abstract class BaseAgent : MonoBehaviour
{
    private int _curFinishCount = 0;
    private float _roundStartTIme = 0f;
    private Vector3 _sceneCenter;

    protected Dictionary<Grabbable, bool> _environmentGrabbablesState;
    protected Vector3[] _initialGrabbablePositions;
    protected Quaternion[] _initialGrabbableRotations;
    protected NavMeshAgent _navMeshAgent;
    protected SceneAnalyzer _sceneAnalyzer;
    protected NavMeshTriangulation _triangulation;
    protected Vector3[] _meshCenters;

    public List<Grabbable> sceneGrabbables;      //�����еĿ�ץȡ����
    public bool drag = false;

    public Grabbable nextGrabbable;     // ����Ŀ�ץȡ����
    public HandController handController;
    public float areaDiameter = 7.5f;
    public float twitchRange = 8f;
    public float moveSpeed = 6f;
    public bool randomGrabble = false;
    public Action roundFinishEvent;

    protected IEnumerator MoveToNextGrabbable()
    {
        GetNextGrabbable(out nextGrabbable);

        if(nextGrabbable != null)
        {
            _navMeshAgent.SetDestination(nextGrabbable.transform.position);  // ����Ŀ��λ��Ϊ����Ŀ�ץȡ����
        }
        _navMeshAgent.speed = moveSpeed;

        float maxTimeout = 30f; // ��������ʱ�䣨�룩������������ʱ�仹û����Ŀ�꣬����Ϊ�ɹ�
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
            yield return StartCoroutine(Drag());
        }

        if(_environmentGrabbablesState.Values.All(value => value)) // �������ֵ��Ϊ true
        {
            roundFinishEvent.Invoke();
            yield return null;
        }

        StartCoroutine(MoveToNextGrabbable());
    }


    /// <summary>
    /// �϶�
    /// </summary>
    /// <returns></returns>
    protected IEnumerator Drag()
    {
        Debug.Log($"Start dragging Objects: {nextGrabbable.name}");

        #region Randomly Walking

        Vector3 randomPosition = _sceneCenter;
        int maxAttempts = 10;
        int attempts = 0;

        while(attempts < maxAttempts)
        {
            float randomOffsetX = Random.Range(twitchRange / 2, twitchRange);
            float randomOffsetZ = Random.Range(twitchRange / 2, twitchRange);
            randomOffsetX = Random.Range(-1, 1) >= 0 ? randomOffsetX : -randomOffsetX;
            randomOffsetZ = Random.Range(-1, 1) >= 0 ? randomOffsetZ : -randomOffsetZ;
            randomPosition = transform.position + new Vector3(randomOffsetX, 0, randomOffsetZ);
            NavMeshPath path = new NavMeshPath();
            if(NavMesh.CalculatePath(transform.position, randomPosition, NavMesh.AllAreas, path))
            {
                if(path.status == NavMeshPathStatus.PathComplete)
                {
                    Debug.Log($"Successfully Finding the path for randomly walking");
                    break;
                }
            }
            attempts++;
        }



        float randomRotationY = Random.Range(-30f, 30f);
        transform.Rotate(0, randomRotationY, 0);
        _navMeshAgent.SetDestination(randomPosition);
        _navMeshAgent.speed = moveSpeed * 0.6f;

        Debug.Log($"Start Randomly Walking");

        while(_navMeshAgent.pathPending || _navMeshAgent.remainingDistance > 0.6f)
        {
            yield return null;
        }

        #endregion

        if(handController.grabber.HoldingItem)
        {
            handController.grabber.TryRelease();
        }

        Debug.Log($"Finish dragging Objects: {nextGrabbable.name}");
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
                sceneGrabbables[i].transform.position = _meshCenters[Random.Range(0, _meshCenters.Length - 1)] + new Vector3(0, 2.5f, 0);
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


    protected abstract void GetNextGrabbable(out Grabbable nextGrabbbable);

    /// <summary>
    /// ͨ����ȡNavMesh���������������񶥵����꣬����ÿ��Mesh�ļ������ġ�������������
    /// </summary>
    /// <returns>NavMesh�Ľ�������</returns>
    private void ParseNavMesh(out Vector3 center, out float radius, out Vector3[] meshCenters)
    {
        int length = _triangulation.vertices.Length / 3;
        center = Vector3.zero;
        meshCenters = new Vector3[length];


        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        Vector3 meshCenter = Vector3.zero;
        int vecticesIndex = 0;

        foreach(Vector3 vertex in _triangulation.vertices)
        {
            center += vertex;
            meshCenter += vertex;
            min = Vector3.Min(min, vertex);
            max = Vector3.Max(max, vertex);
            vecticesIndex += 1;
            if(vecticesIndex % 3 == 0)
            {
                meshCenters[vecticesIndex / 3 - 1] = meshCenter / 3f;
                meshCenter = Vector3.zero;
            }
        }
        center /= length;
        radius = Vector3.Distance(min, max) / 2;

    }

    private void Start()
    {
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _sceneAnalyzer = GetComponent<SceneAnalyzer>();
        _triangulation = NavMesh.CalculateTriangulation();

        ParseNavMesh(out _sceneCenter, out areaDiameter, out _meshCenters);
        GetSceneGrabbables(out sceneGrabbables, out _environmentGrabbablesState);

        StoreSceneGrabbableObjects();
        ResetSceneGrabbableObjects();

        roundFinishEvent += ResetSceneGrabbableObjects;
        roundFinishEvent += () =>
        {
            _curFinishCount += 1;
            Debug.Log($"Round {_curFinishCount} Finished ");
        };

        StartCoroutine(MoveToNextGrabbable());
    }

}