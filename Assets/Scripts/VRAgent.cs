using BNG;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Machine Learning Agent��ǿ��ѧϰ������
/// </summary>
public class VRAgent : Agent
{
    private Grabbable[] _environmentGrabbables;      //�����еĿ�ץȡ����

    private Vector3[] _initialGrabbablePositions;   //��ץȡ����ĳ�ʼλ��
    private Quaternion[] _initialGrabbableRotations;//��ץȡ����ĳ�ʼ��ת
    private Vector3 _initialPosition;       // Agent�ĳ�ʼλ��
    private Quaternion _initialRotation;    // Agent�ĳ�ʼ��ת

    public SmoothLocomotion smoothLocomotion;
    public BNGPlayerController player;
    public Transform rightHandGrabber;      // ���ֱ任
    public Transform leftHandGrabber;       // ���ֱ任
    public Grabbable neareastGrabbable;     // ����Ŀ�ץȡ����

    [Tooltip("�Ƿ�����ѵ��ģʽ�£�trainingMode��")]
    public bool trainingMode;

    public float AreaDiameter = 40f;    // �����İ뾶��С����

    /// <summary>
    /// �Ѿ���ɵ�ץȡ����
    /// </summary>
    public int GrabbablerGrabbed
    {
        get;
        private set;
    }

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
        player = FindObjectOfType<BNGPlayerController>();
        smoothLocomotion = player.GetComponentInChildren<SmoothLocomotion>();

        leftHandGrabber = GameObject.Find("LeftController").transform.GetChild(2);
        rightHandGrabber = GameObject.Find("RightController").transform.GetChild(2);


        _environmentGrabbables = GetEnvironmentGrabbables();
        neareastGrabbable = GetNearestGrabbable();

        StoreAllGrabbableObjectsTransform();   // ���泡���У���ץȡ����ĳ�ʼλ�ú���ת
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
        GrabbablerGrabbed = 0;                     //����ץȡ������

        // ���ü������п�ץȡ�����λ�ú���ת
        LoadAllGrabbableObjectsTransform();


        transform.position = _initialPosition; // �趨��ʼλ��
        transform.rotation = _initialRotation;  // ������ת

        //base.OnEpisodeBegin();
        if(trainingMode)
        {
            //��ѵ��ģʽ�¡���ÿ��������flowerArea��ֻ��һ��������Agent��ʱ��
            //��ʱ��һֻ������һ�����������ϡ�
        }
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
        //if(frozen) return;
        ////��ȡ������Ϊ������
        //var vectorAction = actions.ContinuousActions;
        ////����Ŀ���ƶ�����, targetDirection(dx,dy,dz)
        //Vector3 targetMoveDirection = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);
        ////�����С���ƶ������ϣ�ʩ��һ����
        //rigidbody?.AddForce(targetMoveDirection * moveForce);

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

        ////��ӣ������֣�ָ�������ץȡ�����������(3)
        Vector3 rightHandToNeareastGrabbable = neareastGrabbable.transform.position - rightHandGrabber.transform.position;


        //��ӣ��ֵ������ץȡ�������ԣ���Գ���������(1��
        float relativeDistance = rightHandToNeareastGrabbable.magnitude / AreaDiameter;


        sensor.AddObservation(relativeRotation);
        sensor.AddObservation(rightHandToNeareastGrabbable.normalized);
        sensor.AddObservation(relativeDistance);

        // ��ӣ������ֵİ���״̬(4)
        sensor.AddObservation(InputBridge.Instance.LeftGrip);
        sensor.AddObservation(InputBridge.Instance.RightGrip);
        sensor.AddObservation(InputBridge.Instance.LeftTrigger);
        sensor.AddObservation(InputBridge.Instance.RightTrigger);

        //�ܹ�12���۲�

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
    }

    /// <summary>
    /// ����ҿ���ģʽ�£����ƶ���������
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "ѵ��ģʽ��֧�ֶ��������塣");
        rigidbody?.Sleep();
    }

    /// <summary>
    /// ����ҿ���ģʽ�£��ⶳ������
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "ѵ��ģʽ��֧�ֽⶳ�����塣");
        rigidbody?.WakeUp();
    }

    /// <summary>
    /// ��ȡ���������еĿ�ץȡ�����б�
    /// </summary>
    private Grabbable[] GetEnvironmentGrabbables()
    {
        var allGrabbables = Object.FindObjectsOfType<Grabbable>();
        var environmentGrabbables = allGrabbables.Except(transform.GetComponentsInChildren<Grabbable>()).ToArray();
        Debug.Log($"�����еĿɽ���������{environmentGrabbables.Length}��");
        return environmentGrabbables;
    }

    /// <summary>
    /// �洢���г����п�ץȡ����ı任��Ϣ
    /// </summary>
    private void StoreAllGrabbableObjectsTransform()
    {
        _initialGrabbablePositions = new Vector3[_environmentGrabbables.Length];
        _initialGrabbableRotations = new Quaternion[_environmentGrabbables.Length];

        for(int i = 0; i < _environmentGrabbables.Length; i++)
        {
            _initialGrabbablePositions[i] = _environmentGrabbables[i].transform.position;
            _initialGrabbableRotations[i] = _environmentGrabbables[i].transform.rotation;
        }
    }

    /// <summary>
    /// ���ü������п�ץȡ�����λ�ú���ת
    /// </summary>
    private void LoadAllGrabbableObjectsTransform()
    {
        for(int i = 0; i < _environmentGrabbables.Length; i++)
        {
            _environmentGrabbables[i].transform.position = _initialGrabbablePositions[i];
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
    private Grabbable GetNearestGrabbable()
    {
        var res = _environmentGrabbables.OrderBy(grabbable => Vector3.Distance(leftHandGrabber.position, grabbable.transform.position)).FirstOrDefault();
        if(res == null)
        {
            _environmentGrabbables = GetEnvironmentGrabbables();
            Debug.LogWarning("���ֿ�ץȡ�������ö�ʧ");
            return GetNearestGrabbable();
        }
        return res;
    }

    private void Update()
    {
        //Debug.Log(InputBridge.Instance.RightTrigger);   // �۶����
        //Debug.Log(InputBridge.Instance.RightGrip);      // ץȡ
        //Debug.Log(InputBridge.Instance.RightThumbNear); // ��Ĵָ����
        //InputBridge.Instance.RightGrip = 1f;

        neareastGrabbable = GetNearestGrabbable();
    }

    private void FixedUpdate()
    {
        //smoothLocomotion.MoveCharacter(Vector3.forward *  Time.deltaTime);

    }

    private void Start()
    {
        // ��ȡ��ǰ���弰������������� Collider
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach(Collider col in colliders)
        {
            // ���ֲ�Grabber����ײ�¼�
            if(col.gameObject.name == "Grabber")
            {
                var collisionHandler = col.gameObject.AddComponent<GrabberCollisionHandler>();
                collisionHandler.vrAgent = this;
            }
        }
    }

    // �ֲ���ײʱ����
    public void OnGrabberCollisionEnter(Collision collision)
    {
    }

    // �����崥��ʱ����
    public void OnGrabberTriggerEnter(Collider collider)
    {
        if(collider.transform.GetComponent<Grabbable>() != null && trainingMode)
        {
            //AddReward(-0.5f);
            Debug.Log("��ײGrabbable����");
        }
        if(collider.transform.GetComponent<Grabbable>() != null && !trainingMode)
        {
            //AddReward(-0.5f);
            Debug.Log("��trainingMode����ײGrabbable����");
        }
    }
}