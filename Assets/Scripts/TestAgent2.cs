using BNG;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Machine Learning Agent��ǿ��ѧϰ������
/// </summary>
public class TestAgent2 : Agent
{
    private List<GameObject> _environmentGrabbables;      //�����еĿ�ץȡ����
    private Vector3[] _initialGrabbablePositions;   //��ץȡ����ĳ�ʼλ��
    private Quaternion[] _initialGrabbableRotations;//��ץȡ����ĳ�ʼ��ת
    private Vector3 _initialPosition;       // Agent�ĳ�ʼλ��
    private Quaternion _initialRotation;    // Agent�ĳ�ʼ��ת

    public Transform itemRoot;
    public GameObject neareastGrabbable;     // ����Ŀ�ץȡ����
    public float collisionReward = 5f;  // ץȡ����
    public float boundaryPunishment = -1f;

    [Tooltip("�Ƿ�����ѵ��ģʽ�£�trainingMode��")]
    public bool trainingMode;

    public float AreaDiameter = 20f;    // �����İ뾶��С����

    private float smoothPitchSpeedRate = 0f;
    private float smoothYawSpeedRate = 0f;
    private float smoothChangeRate = 2f;
    private float pitchSpeed = 100f;
    private float maxPitchAngle = 80f;       //��󸩳�Ƕ�
    private float yawSpeed = 100f;
    private float moveForce = 4f;

    private new Rigidbody rigidbody;
    private NavMeshAgent navMeshAgent;  // ���� NavMeshAgent

    /// <summary>
    /// ��ʼ��������
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        rigidbody = GetComponent<Rigidbody>();
        navMeshAgent = GetComponent<NavMeshAgent>();  // ��ȡ NavMeshAgent ���

        GetEnvironmentGrabbables(out _environmentGrabbables);
        GetNearestGrabbable(out neareastGrabbable);

        StoreAllGrabbableObjects();   // ���泡���У���ץȡ����ĳ�ʼλ�ú���ת
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        if(!trainingMode)
        {
            MaxStep = 0;         //��ѵ��ģʽ�£�����������ƣ�MaxStep=0��
        }
    }

    /// <summary>
    /// ��һ��ѵ���غ�(Episode)��ʼ��ʱ���������������
    /// </summary>
    public override void OnEpisodeBegin()
    {
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        ResetAllGrabbableObjects();
        transform.SetPositionAndRotation(_initialPosition, _initialRotation);
    }

    /// <summary>
    /// ��ÿ��Agent���յ�����Ϊʱ����
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        var vectorAction = actions.ContinuousActions;
        Vector3 targetMoveDirection = new Vector3(vectorAction[0], 0, vectorAction[1]);

        // ����Ŀ��λ�ò����� NavMeshAgent ��Ŀ��λ��
        if(neareastGrabbable != null)
        {
            navMeshAgent.SetDestination(neareastGrabbable.transform.position);  // ����Ŀ��λ��Ϊ����Ŀ�ץȡ����
        }

        // ʹ�� NavMeshAgent ���ƶ�
        if(navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.Move(targetMoveDirection * moveForce * Time.deltaTime);
        }
    }

    /// <summary>
    /// �����������ռ��۲����ݵ���Ϊ
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        Quaternion relativeRotation = transform.localRotation.normalized;
        Vector3 ToNeareastGrabbable = neareastGrabbable.transform.position - transform.position;
        float relativeDistance = ToNeareastGrabbable.magnitude / AreaDiameter;

        sensor.AddObservation(relativeRotation);
        sensor.AddObservation(ToNeareastGrabbable.normalized);
        sensor.AddObservation(relativeDistance);
    }

    /// <summary>
    /// ��ȡ���������еĿ�ץȡ�����б�
    /// </summary>
    private void GetEnvironmentGrabbables(out List<GameObject> grabbables)
    {
        grabbables = new List<GameObject>();
        foreach(Transform child in itemRoot.transform)
        {
            grabbables.Add(child.gameObject);
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
            _environmentGrabbables[i].gameObject.SetActive(true);
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
    private void GetNearestGrabbable(out GameObject nearestGrabbable)
    {
        nearestGrabbable = _environmentGrabbables
            .Where(grabbable => grabbable.activeInHierarchy)
            .OrderBy(grabbable => Vector3.Distance(transform.position, grabbable.transform.position))
            .FirstOrDefault();
    }

    private void Update()
    {
        GetNearestGrabbable(out neareastGrabbable);
    }

    private void FixedUpdate()
    {
    }

    private void OnCollisionEnter(Collision collision)
    {

        if(collision.gameObject.CompareTag("Grabbable"))
        {
            AddReward(collisionReward);
            collision.gameObject.SetActive(false);

            if(_environmentGrabbables.Count(grabbable => grabbable.activeInHierarchy) == 0)
            {
                if(trainingMode)
                {
                    EndEpisode();
                }
                else
                {
                    ResetAllGrabbableObjects();
                }
            }
            Debug.Log("collisionReward");
        }
        else if(collision.gameObject.CompareTag("Boundary"))
        {
            AddReward(boundaryPunishment);
            Debug.Log("boundaryPunishment");
        }
    }

}
