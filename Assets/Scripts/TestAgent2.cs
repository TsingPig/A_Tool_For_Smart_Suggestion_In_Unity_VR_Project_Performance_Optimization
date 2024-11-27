using BNG;

using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Machine Learning Agent��ǿ��ѧϰ������
/// </summary>
public class TestAgent2: Agent
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


    //���������ж������������ͬ���ĳ�Ա�����������˻���ĳ�Ա��
    //ʹ��new�ؼ�����ʾ�����ػ����еĳ�Ա
    private new Rigidbody rigidbody;

    /// <summary>
    /// ��ʼ��������
    /// </summary>
    public override void Initialize()
    {
        //override��дvirtual��������base.Initialize()��ʾ���ñ���д��������෽��
        //��������൱�ڶ��鷽�����й��ܵ����䡣
        base.Initialize();
        rigidbody = GetComponent<Rigidbody>();

        GetEnvironmentGrabbables(out _environmentGrabbables);
        GetNearestGrabbable(out neareastGrabbable);

        StoreAllGrabbableObjects();   // ���泡���У���ץȡ����ĳ�ʼλ�ú���ת
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        //MaxStep����������ѵ��ģʽ�£���ĳ���������ܹ�ִ�е������
        if(!trainingMode)
        {
            MaxStep = 0;         //��ѵ��ģʽ�£�����������ƣ�MaxStep=0��
        }
    }

    /// <summary>
    /// ��һ��ѵ���غ�(Episode)��ʼ��ʱ���������������
    /// ���ǻὫ����������ٶ�״̬������״̬��������ѵ��ģʽ�£�����С������
    /// �Ļ�������<see cref="FlowerArea"/>����������Ƿ�Ҫ�ڻ�ǰ���ѡ�
    /// Ȼ���ƶ����µ����λ�á������¼��㵱ǰ����Ļ���
    /// </summary>
    public override void OnEpisodeBegin()
    {
        rigidbody.velocity = Vector3.zero;       //���ٶȺͽ��ٶȹ���
        rigidbody.angularVelocity = Vector3.zero;

        // ���ü������п�ץȡ�����λ�ú���ת
        ResetAllGrabbableObjects();
        transform.SetPositionAndRotation(_initialPosition, _initialRotation);
    }

    /// <summary>
    /// ��ÿ��Agent����������ҡ��������������ʽ�ľ���ʵ�壩���յ�һ������Ϊ��ʱ����á�(action received)
    /// ���ݽ��յ�����Ϊ����Agent��״̬��ִ���ض��Ķ����򴥷���ص��¼���
    /// �������������ݲ�ͬ����Ϊ������Agent����Ϸ�н�����Ӧ�Ĳ����;��ߣ�
    /// ��ʵ�����������Ϊ���ƺ�ѧϰ��
    /// Index 0:x����ı�����+1=���ң�-1=����
    /// Index 1:y����ı���(+1=up,-1=down)
    /// Index 2:z����ı���(+1=forward,-1=backward)
    /// Index 3:����Ƕȸı���(+1=pitch up,-1=pitch down)
    /// Index 4:ƫ���Ƕȸı���(+1=yaw turn right,-1=yaw turn left)
    /// </summary>
    /// <param name="actions">ActionBuffers���Ͷ���
    /// ���ڴ洢���յ�����Ϊ����Ϣ��ͨ������actions��������Ժͷ�����
    /// ���Ի�ȡ�ͽ�����Ϊ�ľ������ݣ�
    /// ����λ�ơ���ת�������ȡ�</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        var vectorAction = actions.ContinuousActions;
        //����Ŀ���ƶ�����, targetDirection(dx,dy,dz)
        Vector3 targetMoveDirection = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);
        //�����С���ƶ������ϣ�ʩ��һ����
        rigidbody.AddForce(targetMoveDirection * moveForce);


        ////��õ�ǰ��ת��״̬(������ת�ĽǶȶ���ŷ���ǣ�������������ת��ŷ����
        //Vector3 curRotation = transform.rotation.eulerAngles;

        ////��������Ϊ�м��㸩����ٶ��ʣ�-1~1����ƫ�����ٶ��ʣ�-1~1��
        //float targetPitchSpeedRate = vectorAction[3];
        //float targetYawSpeedRate = vectorAction[4];

        ////ƽ�����㣬��smoothƽ��������ɵ�targetDelta�ϡ�
        ////smooth���м���̴���ǰ�Ѿ����㵽�ġ�Ӧ�ø��ӵı仯����
        //smoothPitchSpeedRate = Mathf.MoveTowards(smoothPitchSpeedRate, targetPitchSpeedRate, smoothChangeRate * Time.fixedDeltaTime);
        //smoothYawSpeedRate = Mathf.MoveTowards(smoothYawSpeedRate, targetYawSpeedRate, smoothChangeRate * Time.fixedDeltaTime);
        ////p+=Rdp*dp*dt,y=Rdy*dy*dt
        //float pitch = curRotation.x + smoothPitchSpeedRate * Time.fixedDeltaTime * pitchSpeed;
        //float yaw = curRotation.y + smoothYawSpeedRate * Time.fixedDeltaTime * yawSpeed;
        //if(pitch > 180f) pitch -= 360f;
        //pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);

        ////������󣬽��µõ�����ת�Ƕȸ��ǵ���ǰ��ת״̬��
        //transform.rotation = Quaternion.Euler(pitch, yaw, 0);


    }

    /// <summary>
    /// �����������ռ��۲����ݵ���Ϊ����λ�ȡ�ʹ���۲����ݣ�
    /// �Ա����ѵ���;��ߡ�
    /// </summary>
    /// <param name="sensor">�������������������������������嵱ǰ״̬����������Ϣ��
    /// ���������۲�����///</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        ////sensor.AddObservation(�۲�����)���ڽ��۲�������ӵ��������֪��������ѵ��������

        //��ӣ�����ڸ�����ľֲ���ת���������С������ת��4��
        //��λ��Ԫ���ǳ���Ϊ1����Ԫ�������ڱ�ʾ��ת����
        Quaternion relativeRotation = transform.localRotation.normalized;
        ////��ӣ���λ��ָ�������ץȡ�����������(3)


        Vector3 ToNeareastGrabbable = neareastGrabbable.transform.position - transform.position;
        //��ӣ��ֵ������ץȡ�������ԣ���Գ���������(1��
        float relativeDistance = ToNeareastGrabbable.magnitude / AreaDiameter;
        sensor.AddObservation(relativeRotation);
        sensor.AddObservation(ToNeareastGrabbable.normalized);
        sensor.AddObservation(relativeDistance);

        //�ܹ�8���۲�

    }

    /// <summary>
    /// �����������Ϊ�������ͱ�����Ϊ"Heuristic Only"�������������
    /// ����ֵ�������ݵ�<see cref="OnActionReceived(ActionBuffers)"/>
    /// ������ʽģʽ�£��ֶ���д���������Ϊ�߼�������ʽ�㷨����������
    /// Index 0:x����ı�����+1=���ң�-1=����
    /// Index 1:y����ı���(+1=up,-1=down)
    /// Index 2:z����ı���(+1=forward,-1=backward)
    /// Index 3:����Ƕȸı���(+1=pitch up,-1=pitch down)
    /// Index 4:ƫ���Ƕȸı���(+1=yaw turn right,-1=yaw turn left)
    /// </summary>
    /// <param name="actionsOut">�洢���������Ϊ���</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Vector3 left = Vector3.zero;     //x
        Vector3 up = Vector3.zero;       //y
        Vector3 forward = Vector3.zero; //z
        float pitch = 0f;
        float yaw = 0f;
        //�û��������
        //���û������ʾ���ƶ�����ת��ӳ�䵽����������͸�����
        if(Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if(Input.GetKey(KeyCode.S)) forward = (-1f) * transform.forward;

        if(Input.GetKey(KeyCode.LeftArrow)) left = (-1f) * transform.right;
        else if(Input.GetKey(KeyCode.RightArrow)) left = transform.right;

        if(Input.GetKey(KeyCode.Space)) left = transform.up;
        else if(Input.GetKey(KeyCode.LeftControl)) left = (-1f) * transform.up;

        if(Input.GetKey(KeyCode.UpArrow)) pitch = -1f;
        else if(Input.GetKey(KeyCode.DownArrow)) pitch = 1f;

        if(Input.GetKey(KeyCode.A)) yaw = -1f;
        else if(Input.GetKey(KeyCode.D)) yaw = 1f;

        Vector3 combinedDirection = (forward + up + left).normalized;

        actionsOut.ContinuousActions.Array[0] = combinedDirection.x;
        actionsOut.ContinuousActions.Array[1] = combinedDirection.y;
        actionsOut.ContinuousActions.Array[2] = combinedDirection.z;
        //actionsOut.ContinuousActions.Array[3] = pitch;
        //actionsOut.ContinuousActions.Array[4] = yaw;

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

        //Debug.Log($"�����еĿɽ���������{grabbables.Count}��");
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
            //_environmentGrabbables[i].transform.position = _initialGrabbablePositions[i];
            //_environmentGrabbables[i].transform.rotation = _initialGrabbableRotations[i];


            // ���������ƫ�������� -AreaDiameter �� +AreaDiameter ��Χ�ڣ�
            float randomX = Random.Range(-AreaDiameter, AreaDiameter);
            float randomZ = Random.Range(-AreaDiameter, AreaDiameter);
            float randomY = 1f;  // �̶����䣬�����Ҫ��̬�ı�yֵ������ʹ�����Ƶ������ʽ��Random.Range(minY, maxY)

            // �����µ�Ŀ��λ�ã����� itemRoot.position �����ƫ�ƣ�
            Vector3 newPosition = itemRoot.position + new Vector3(randomX, randomY, randomZ);
            _environmentGrabbables[i].transform.position = newPosition;
            _environmentGrabbables[i].transform.rotation = _initialGrabbableRotations[i];

            // ����� Rigidbody��������ٶ�
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
            .Where(grabbable => grabbable.activeInHierarchy)  // ֻѡ�񼤻�״̬������
            .OrderBy(grabbable => Vector3.Distance(transform.position, grabbable.transform.position))  // ����������
            .FirstOrDefault();  // ��ȡ��������壨����У�
    }


    private void Update()
    {
        GetNearestGrabbable(out neareastGrabbable);
    }

    private void FixedUpdate()
    {

    }

    private void Start()
    {
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(trainingMode)
        {
            if(collision.gameObject.CompareTag("Grabbable"))
            {
                AddReward(collisionReward);
                collision.gameObject.SetActive(false);
                if(_environmentGrabbables.Count(grabbable => grabbable.activeInHierarchy) == 0)
                {
                    EndEpisode();
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

}