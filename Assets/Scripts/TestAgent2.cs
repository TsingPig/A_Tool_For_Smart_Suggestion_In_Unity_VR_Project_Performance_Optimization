using BNG;
using System.Collections;
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
    private List<Grabbable> _environmentGrabbables;      //�����еĿ�ץȡ����
    private Dictionary<Grabbable, bool> _environmentGrabbablesState;
    private Vector3[] _initialGrabbablePositions;   //��ץȡ����ĳ�ʼλ��
    private Quaternion[] _initialGrabbableRotations;//��ץȡ����ĳ�ʼ��ת
    private Vector3 _initialPosition;       // Agent�ĳ�ʼλ��
    private Quaternion _initialRotation;    // Agent�ĳ�ʼ��ת



    public float curReward = 0f;

    public Transform itemRoot;
    public Grabbable neareastGrabbable;     // ����Ŀ�ץȡ����
    public HandController handController;

    public float grabReward = 5f;  // ץȡ����
    public float grabbingReward = 0.01f;
    public float releaseReward = 3f;



    private bool dragging = false;


    [Tooltip("�Ƿ�����ѵ��ģʽ�£�trainingMode��")]
    public bool trainingMode;

    public float AreaDiameter = 7.5f;    // �����İ뾶��С����


    public float moveSpeed = 4f;

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

        GetEnvironmentGrabbables(out _environmentGrabbables, out _environmentGrabbablesState);
        GetNearestGrabbable(out neareastGrabbable);

        StoreAllGrabbableObjects();   // ���泡���У���ץȡ����ĳ�ʼλ�ú���ת
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        if(!trainingMode)
        {
            MaxStep = 0;         //��ѵ��ģʽ�£�����������ƣ�MaxStep=0��
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        if(Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space");
            continuousActions[0] = 1;
        }
        if(Input.GetKeyDown(KeyCode.LeftControl))
        {
            continuousActions[0] = 0;
        }
    }

    /// <summary>
    /// ��һ��ѵ���غ�(Episode)��ʼ��ʱ���������������
    /// </summary>
    public override void OnEpisodeBegin()
    {
        curReward = 0f;

        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        ResetAllGrabbableObjects();
        transform.SetPositionAndRotation(_initialPosition, _initialRotation);

        if(neareastGrabbable != null)
        {
            navMeshAgent.SetDestination(neareastGrabbable.transform.position);  // ����Ŀ��λ��Ϊ����Ŀ�ץȡ����
        }
    }

    /// <summary>
    /// ��ÿ��Agent���յ�����Ϊʱ����
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {

        if(dragging) { return; }

        var continuousActions = actions.ContinuousActions;
        Debug.Log(continuousActions[0]);
        if(continuousActions[0] == 0)
        {
            // 0 ���ɿ�
            if(handController.grabber.HoldingItem)
            {
                handController.grabber.TryRelease();
                AddReward(releaseReward);
                curReward += releaseReward;
            }
        }
        else
        {
            Debug.Log("discreteActions = 1");
            // 1 ��ץȡ
            if(!handController.grabber.HoldingItem && Vector3.Distance(transform.position, neareastGrabbable.transform.position) < 2f)    // ����ץȡ״̬��
            {

                handController.grabber.GrabGrabbable(neareastGrabbable);

                if(handController.grabber.HoldingItem)    // �ɹ�ץȡ
                {
                    Debug.Log("�ɹ�ץȡ");
                    _environmentGrabbablesState[neareastGrabbable] = true;
                    AddReward(grabReward);
                    curReward += grabReward;
                    StartCoroutine(Drag(Random.Range(3f, 6f))); // ��ʼ��ק
                }
            };
        }
    }


    /// <summary>
    /// ����鴤
    /// </summary>
    private void RandomTwitch()
    {
        // ��������鴤�ķ�Χ
        float twitchRange = 6f; // ����鴤�İ뾶��Χ
        float randomOffsetX = Random.Range(-twitchRange, twitchRange); // X��������ƫ��
        float randomOffsetZ = Random.Range(-twitchRange, twitchRange); // Z��������ƫ��

        // ������λ�ã��ڵ�ǰλ�õĸ�������鴤��
        Vector3 randomPosition = transform.position + new Vector3(randomOffsetX, 0, randomOffsetZ);

        // �����ת��ģ��鴤ʱ�����תȦ��
        float randomRotationY = Random.Range(-30f, 30f); // �� -30 �� 30 �ȷ�Χ����ת
        transform.Rotate(0, randomRotationY, 0);

        // ����Ŀ��λ�ã����ʹ�� NavMeshAgent��
        navMeshAgent.SetDestination(randomPosition);
        navMeshAgent.speed = moveSpeed * 0.6f; // �鴤ʱ�ٶȽ���
    }

    /// <summary>
    /// ��ק
    /// </summary>
    /// <param name="dragTime"></param>
    /// <returns></returns>
    IEnumerator Drag(float dragTime = 3f)
    {
        dragging = true;
        RandomTwitch();
        yield return new WaitForSeconds(dragTime);
        if(neareastGrabbable != null)
        {
            navMeshAgent.SetDestination(neareastGrabbable.transform.position);  // ����Ŀ��λ��Ϊ����Ŀ�ץȡ����
        }
        navMeshAgent.speed = moveSpeed;
        dragging = false;
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
            //_environmentGrabbables[i].gameObject.SetActive(true);
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

    private void Update()
    {
        GetNearestGrabbable(out neareastGrabbable);
        if(!_environmentGrabbablesState.Values.ToList().Contains(false)) // ���ж�ץȡ��һ��
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
    }

    private void FixedUpdate()
    {
        if(handController.grabber.HoldingItem)
        {
            AddReward(grabbingReward);
            curReward += grabbingReward;
        }
    }

}
