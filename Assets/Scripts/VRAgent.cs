using BNG;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

/// <summary>
/// Machine Learning Agent��ǿ��ѧϰ������
/// </summary>
public class VRAgent : Agent
{
    private Grabbable[] _grabbables;
    private Vector3[] _initialPositions;
    private Quaternion[] _initialRotations;

    public SmoothLocomotion smoothLocomotion;
    public BNGPlayerController player;

    [Tooltip("�Ƿ�����ѵ��ģʽ�£�trainingMode��")]
    public bool trainingMode;

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

    private const float ModelsHandRadius = 0.008f; //����Grabber��������ײ����

    private bool frozen = false;          //Agent�Ƿ��ڷǷ���״̬

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

        // ��ȡ���������еĿ�ץȡ�����б�
        GetAllGrabbaleObjects();

        // ���泡���У���ץȡ����ĳ�ʼλ�ú���ת
        StoreAllGrabbableObjectsTransform();

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

        transform.position = new Vector3(0, 1, 0); // �趨��ʼλ��
        transform.rotation = Quaternion.identity;  // ������ת

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
        ////������Ļ���û�����ó�����ʱ��Ҫ���ݽ�ȥһ���յ�10άfloat����
        //if(nearestFlower == null)
        //{
        //    sensor.AddObservation(new float[10]);
        //    return;
        //}
        ////��ӣ�����ڸ�����ľֲ���ת���������С������ת��4��
        ////��λ��Ԫ���ǳ���Ϊ1����Ԫ�������ڱ�ʾ��ת����
        //Quaternion relativeRotation = transform.localRotation.normalized;
        ////��ӣ�ָ�򻨵�����(3)
        //Vector3 toFlower = nearestFlower.FlowerCenterPosition - BeakTipCenterPosition;
        ////toFlower.Normalize();
        ////��ӣ��ж������Ƿ��򻨿���(+1����ֱ���ڻ���ǰ��-1�����ڻ����棩(1)
        ////��������ˣ�A dot B > 0��ʾ������ͬ����ʾ�泯���� <0�෴��Ϊ0��ֱ
        //float positionAlignment = Vector3.Dot(toFlower.normalized,
        //    -nearestFlower.FlowerUpVector.normalized);
        ////��ӣ��ж��Ƿ���๳��򻨿���(�������ʾ��๳��򻨿��ڣ�(1)
        ////float beakTipAlignment = Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized);
        ////��ӣ���๵�������ԣ����С�������루1��
        //float relativeDistance = toFlower.magnitude / FlowerArea.areaDiameter;
        //sensor.AddObservation(relativeRotation);
        //sensor.AddObservation(toFlower.normalized);
        //sensor.AddObservation(positionAlignment);
        //sensor.AddObservation(beakTipAlignment);
        //sensor.AddObservation(relativeDistance);
        //�ܹ�10���۲�

        sensor.AddObservation(InputBridge.Instance.LeftGrip);
        sensor.AddObservation(InputBridge.Instance.RightGrip);
        sensor.AddObservation(InputBridge.Instance.LeftTrigger);
        sensor.AddObservation(InputBridge.Instance.RightTrigger);
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
        frozen = true;
        rigidbody?.Sleep();
    }

    /// <summary>
    /// ����ҿ���ģʽ�£��ⶳ������
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "ѵ��ģʽ��֧�ֽⶳ�����塣");
        frozen = false;
        rigidbody?.WakeUp();
    }

    /// <summary>
    /// ��ȡ���������еĿ�ץȡ�����б�
    /// </summary>
    private void GetAllGrabbaleObjects()
    {
        _grabbables = Object.FindObjectsOfType<Grabbable>();
        Debug.Log($"�����еĿɽ���������{_grabbables.Length}��");
    }

    /// <summary>
    /// �洢���г����п�ץȡ����ı任��Ϣ
    /// </summary>
    private void StoreAllGrabbableObjectsTransform()
    {
        _initialPositions = new Vector3[_grabbables.Length];
        _initialRotations = new Quaternion[_grabbables.Length];

        for(int i = 0; i < _grabbables.Length; i++)
        {
            _initialPositions[i] = _grabbables[i].transform.position;
            _initialRotations[i] = _grabbables[i].transform.rotation;
        }
    }

    /// <summary>
    /// ���ü������п�ץȡ�����λ�ú���ת
    /// </summary>
    private void LoadAllGrabbableObjectsTransform()
    {
        for(int i = 0; i < _grabbables.Length; i++)
        {
            _grabbables[i].transform.position = _initialPositions[i];
            _grabbables[i].transform.rotation = _initialRotations[i];

            // ����� Rigidbody��������ٶ�
            Rigidbody rb = _grabbables[i].GetComponent<Rigidbody>();
            if(rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void Update()
    {
        //Debug.Log(InputBridge.Instance.RightTrigger);   // �۶����
        //Debug.Log(InputBridge.Instance.RightGrip);      // ץȡ
        //Debug.Log(InputBridge.Instance.RightThumbNear); // ��Ĵָ����
        //InputBridge.Instance.RightGrip = 1f;
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