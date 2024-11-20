using BNG;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
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

    public bool isGrabbing;


    public SmoothLocomotion smoothLocomotion;
    public BNGPlayerController player;
    public Transform rightHandGrabber;      // ���ֱ任
    //public Transform leftHandGrabber;       // ���ֱ任
    public Grabbable neareastGrabbable;     // ����Ŀ�ץȡ����

    public float grabbedReward = 5f;  // ץȡ����
    public float grabbingReward = 0.005f; // ����ץȡ����
    public float ungrabbedReward = 2f; // ���ֽ���
    public float idlePunishment = -0.0001f;
    public float distancePunisnment = -0.02f;
    public float outOfBoundPunishment = -0.02f;

    /// <summary>
    /// �Ƿ�����ץס����
    /// </summary>
    public bool IsGrabbing
    {
        get { return isGrabbing; }
        set
        {
            if(value)
            {
                GrabbablerGrabbed += 1;
                AddReward(grabbedReward); // ץȡ����
                currentReward += grabbedReward;
                Debug.Log($"ץס�����壬��������{grabbedReward}");
            }
            else
            {
                AddReward(ungrabbedReward); // �ſ�����
                currentReward += ungrabbedReward;
                Debug.Log($"�ſ������壬��������{ungrabbedReward}");
            }
            isGrabbing = value;
        }
    }

    /// <summary>
    /// �Ѿ���ɵ�ץȡ����
    /// </summary>
    public int GrabbablerGrabbed
    {
        get;
        private set;
    }


    public float currentReward = 0f;

    [Tooltip("�Ƿ�����ѵ��ģʽ�£�trainingMode��")]
    public bool trainingMode;

    public float AreaDiameter = 40f;    // �����İ뾶��С����

    private float smoothPitchSpeedRate = 0f;
    private float smoothYawSpeedRate = 0f;
    private float smoothChangeRate = 2f;
    private float pitchSpeed = 200f;
    private float maxPitchAngle = 20f;       //��󸩳�Ƕ�
    private float yawSpeed = 200f;
    private float moveSpeed = 4f;
    private bool frozen = false;          //Agent�Ƿ��ڷ��ƶ�״̬


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

        //leftHandGrabber = GameObject.Find("LeftController").transform.GetChild(2);
        rightHandGrabber = GameObject.Find("RightController").transform.GetChild(2);


        _environmentGrabbables = GetEnvironmentGrabbables();
        neareastGrabbable = GetNearestGrabbable();

        StoreAllGrabbableObjectsTransform();   // ���泡���У���ץȡ����ĳ�ʼλ�ú���ת
        _initialPosition = smoothLocomotion.transform.position;
        _initialRotation = smoothLocomotion.transform.rotation;

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
        currentReward = 0;
        // ���ü������п�ץȡ�����λ�ú���ת
        LoadAllGrabbableObjectsTransform();


        smoothLocomotion.transform.position = _initialPosition; // �趨��ʼλ��
        smoothLocomotion.transform.rotation = _initialRotation;  // ������ת


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
        //��ȡ������Ϊ������
        var continuousActions = actions.ContinuousActions;
        //����Ŀ���ƶ�����, targetDirection(dx,dy,dz)
        Vector3 targetMoveDirection = new Vector3(continuousActions[0], 0, continuousActions[2]);
        // ����Ŀ���ƶ�

        targetMoveDirection = smoothLocomotion.transform.TransformDirection(targetMoveDirection);

        smoothLocomotion.MoveCharacter(targetMoveDirection.normalized * Time.deltaTime * moveSpeed);
        //rigidbody?.AddForce(targetMoveDirection * moveForce);

        //��õ�ǰ��ת��״̬(������ת�ĽǶȶ���ŷ���ǣ�������������ת��ŷ����
        Vector3 curRotation = smoothLocomotion.transform.rotation.eulerAngles;

        //��������Ϊ�м��㸩����ٶ��ʣ�-1~1����ƫ�����ٶ��ʣ�-1~1��
        float targetPitchSpeedRate = continuousActions[3];
        float targetYawSpeedRate = continuousActions[4];

        //ƽ�����㣬��smoothƽ��������ɵ�targetDelta�ϡ�
        //smooth���м���̴���ǰ�Ѿ����㵽�ġ�Ӧ�ø��ӵı仯����
        smoothPitchSpeedRate = Mathf.MoveTowards(smoothPitchSpeedRate, targetPitchSpeedRate, smoothChangeRate * Time.fixedDeltaTime);
        smoothYawSpeedRate = Mathf.MoveTowards(smoothYawSpeedRate, targetYawSpeedRate, smoothChangeRate * Time.fixedDeltaTime);
        //p+=Rdp*dp*dt,y=Rdy*dy*dt
        float pitch = curRotation.x + smoothPitchSpeedRate * Time.fixedDeltaTime * pitchSpeed;
        float yaw = curRotation.y + smoothYawSpeedRate * Time.fixedDeltaTime * yawSpeed;
        if(pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);

        //������󣬽��µõ�����ת�Ƕȸ��ǵ���ǰ��ת״̬��
        smoothLocomotion.transform.rotation = Quaternion.Euler(pitch, yaw, 0);


        InputBridge.Instance.RightTrigger = continuousActions[5] > 0 ? 1f : 0;
        InputBridge.Instance.RightGrip = continuousActions[6] > 0 ? 1f : 0;
        Debug.Log(continuousActions[5]);
        Debug.Log(continuousActions[6]);

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

        // ��ӣ����ֵİ���״̬(2)
        sensor.AddObservation(InputBridge.Instance.RightGrip);
        sensor.AddObservation(InputBridge.Instance.RightTrigger);

        //�ܹ�10���۲�

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
        var continuousActions = actionsOut.ContinuousActions;

        // WASD �ƶ�����
        Vector3 moveDirection = Vector3.zero;
        if(Input.GetKey(KeyCode.W)) moveDirection += Vector3.forward;
        if(Input.GetKey(KeyCode.S)) moveDirection += Vector3.back;
        if(Input.GetKey(KeyCode.A)) moveDirection += Vector3.left;
        if(Input.GetKey(KeyCode.D)) moveDirection += Vector3.right;
        // ��������ת
        float pitch = -Input.GetAxis("Mouse Y"); // ��ֱ��ת�������ӽǣ�
        float yaw = Input.GetAxis("Mouse X");   // ˮƽ��ת�������ӽǣ�

        // Z/X ������ץȡ����
        float rightTrigger = Input.GetKey(KeyCode.Z) ? 1f : 0f;
        float rightGrip = Input.GetKey(KeyCode.X) ? 1f : 0f;

        // ��һ���ƶ�����
        moveDirection = moveDirection.normalized;

        // ������ӳ�䵽 ContinuousActions
        continuousActions[0] = moveDirection.x; // X �����ƶ�
        continuousActions[1] = moveDirection.y; // Y �����ƶ�
        continuousActions[2] = moveDirection.z; // Z �����ƶ�
        continuousActions[3] = pitch;           // ��ֱ�ӽ���ת
        continuousActions[4] = yaw;             // ˮƽ�ӽ���ת
        continuousActions[5] = rightTrigger;
        continuousActions[6] = rightGrip;
        // ������ӳ�䵽 DiscreteActions (ץȡ����)
        //discreteActions[0] = rightTrigger > 0 ? 1 : 0; // ���ץȡ
        //discreteActions[1] = rightGrip > 0 ? 1 : 0;    // �ֱ�ץȡ
    }


    /// <summary>
    /// ����ҿ���ģʽ�£����ƶ���������
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "ѵ��ģʽ��֧�ֶ��������塣");
        //frozen = true;

    }

    /// <summary>
    /// ����ҿ���ģʽ�£��ⶳ������
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "ѵ��ģʽ��֧�ֽⶳ�����塣");
        frozen = false;
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
        _environmentGrabbables = GetEnvironmentGrabbables();
        var res = _environmentGrabbables.OrderBy(grabbable => Vector3.Distance(rightHandGrabber.position, grabbable.transform.position)).FirstOrDefault();
        return res;
    }

    private void Update()
    {
        //Debug.Log(InputBridge.Instance.RightTrigger);   // �۶����
        //Debug.Log(InputBridge.Instance.RightGrip);      // ץȡ
        //Debug.Log(InputBridge.Instance.RightThumbNear); // ��Ĵָ����
        //InputBridge.Instance.RightGrip = 1f;

        neareastGrabbable = GetNearestGrabbable();

        if(trainingMode)
        {
            AddReward(idlePunishment);
            currentReward += idlePunishment;
            if(Vector3.Distance(neareastGrabbable.transform.position, smoothLocomotion.transform.position) > 0.4f * AreaDiameter)
            {
                AddReward(distancePunisnment);
                currentReward += distancePunisnment;
            }

            if(smoothLocomotion.transform.position.y < -5f)
            {
                AddReward(outOfBoundPunishment);
                currentReward += outOfBoundPunishment;

            }
        }

    }

    private void FixedUpdate()
    {
        //smoothLocomotion.MoveCharacter(Vector3.forward *  Time.deltaTime);

    }

    private void Start()
    {
        var handler = rightHandGrabber.gameObject.AddComponent<GrabberCollisionHandler>();
        handler.vrAgent = this;
    }


    /// <summary>
    /// Grabber�Ӵ�����ץȡ����ʱ����
    /// </summary>
    /// <param name="collider"></param>
    public void OnGrabberTriggerEnter(Collider collider)
    {
        Grabbable grabbable = collider.transform.GetComponent<Grabbable>();
        if(_environmentGrabbables.Contains(grabbable) && trainingMode)
        {
        }

    }

    /// <summary>
    /// Grabber�뿪��ץȡ����ʱ����
    /// </summary>
    /// <param name="collider"></param>
    public void OnGrabberTriggerExit(Collider collider)
    {
        Grabbable grabbable = collider.transform.GetComponent<Grabbable>();
        if(_environmentGrabbables.Contains(grabbable) && trainingMode)
        {
            //if(IsGrabbing && InputBridge.Instance.RightGrip < 1f)
            //{
            //    IsGrabbing = false; // ����ץȡ״̬
            //}
        }
    }

    /// <summary>
    /// Grabber�����Ӵ���ץȡ����ʱ����
    /// </summary>
    /// <param name="collider"></param>
    public void OnGrabberTriggerStay(Collider collider)
    {
        Grabbable grabbable = collider.transform.GetComponent<Grabbable>();
        if(_environmentGrabbables.Contains(grabbable) && trainingMode)
        {
            // �������ץȡ״̬������ץȡ����
            if(InputBridge.Instance.RightGrip == 1f)
            {
                if(!IsGrabbing)
                {
                    IsGrabbing = true; // ����ץȡ״̬
                }
                else
                {
                    // ���ڳ���ץȡ״̬
                    AddReward(grabbingReward); // ץȡ����
                    currentReward += grabbingReward;
                    Debug.Log($"����ץȡ����������{grabbingReward}");
                }
            }
            // �����ץȡ״̬�л�����ץȡ״̬������ſ�����
            else if(IsGrabbing && InputBridge.Instance.RightGrip < 1f)
            {
                IsGrabbing = false; // ����ץȡ״̬
            }

        }
    }


}