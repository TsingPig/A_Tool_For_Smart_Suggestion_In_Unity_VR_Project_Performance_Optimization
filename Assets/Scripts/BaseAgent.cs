using BNG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.XR.Interaction.Toolkit;
using Random = UnityEngine.Random;

namespace VRAgent
{
    public abstract class BaseAgent : MonoBehaviour
    {
        private int _curFinishCount = 0;
        private Vector3 _sceneCenter;
        protected Dictionary<Grabbable, bool> _environmentGrabbablesState;
        protected Vector3[] _initialGrabbablePositions;
        protected Quaternion[] _initialGrabbableRotations;
        protected NavMeshAgent _navMeshAgent;
        protected NavMeshTriangulation _triangulation;
        protected Vector3[] _meshCenters;
        public List<Grabbable> sceneGrabbables;      //�����еĿ�ץȡ����
        public bool drag = false;
        public Grabbable nextGrabbable;     // ����Ŀ�ץȡ����
        public HandController leftHandController;
        public XRBaseInteractor rightHandController;
        public float areaDiameter = 7.5f;
        public float twitchRange = 8f;
        public float moveSpeed = 6f;
        public bool randomGrabble = false;
        public Action roundFinishEvent;

        protected async Task MoveToNextGrabbable()
        {
            SceneAnalyzer.Instance.ShowMetrics();
            GetNextGrabbable(out nextGrabbable);

            if(nextGrabbable == null) return;

            MoveAction moveAction = new MoveAction(_navMeshAgent, nextGrabbable.transform.position, moveSpeed);
            await moveAction.Execute();

            _environmentGrabbablesState[nextGrabbable] = true;

            if(drag && nextGrabbable)
            {
                GrabAction grabAction = new GrabAction(leftHandController, nextGrabbable);
                grabAction.Grab();

                var dragAction = new DragAction(_navMeshAgent, nextGrabbable.transform, _sceneCenter, moveSpeed * 0.6f);
                await dragAction.Execute();

                grabAction.Release();
            }

            if(_environmentGrabbablesState.Values.All(value => value))
            {
                roundFinishEvent.Invoke();
            }
            else
            {
                await MoveToNextGrabbable();
            }
        }

        protected abstract void GetNextGrabbable(out Grabbable nextGrabbable);


        /// <summary>
        /// ��ȡ���������еĿ�ץȡ�����б�
        /// </summary>
        /// <param name="grabbables">��ץȡ�����б�</param>
        /// <param name="grabbableState">��ץȡ����״̬</param>
        protected void GetSceneGrabbables(out List<Grabbable> grabbables, out Dictionary<Grabbable, bool> grabbableState)
        {
            grabbables = new List<Grabbable>();
            grabbableState = new Dictionary<Grabbable, bool>();

            foreach(GameObject grabbableObject in SceneAnalyzer.Instance.grabbableObjects)
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
            for(int i = 0; i < sceneGrabbables.Count; i++)
            {
                _environmentGrabbablesState[sceneGrabbables[i]] = false;

                if(randomGrabble)
                {
                    sceneGrabbables[i].transform.position = _meshCenters[Random.Range(0, _meshCenters.Length - 1)] + new Vector3(0, 5f, 0);
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

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
            SceneAnalyzer.Instance.RegisterAllEntities();
        }

        private void Start()
        {
            _triangulation = NavMesh.CalculateTriangulation();
            ParseNavMesh(out _sceneCenter, out areaDiameter, out _meshCenters);
            GetSceneGrabbables(out sceneGrabbables, out _environmentGrabbablesState);

            StoreSceneGrabbableObjects();
            ResetSceneGrabbableObjects();
            roundFinishEvent += () =>
            {
                SceneAnalyzer.Instance.ShowMetrics();

                ResetSceneGrabbableObjects();
                _curFinishCount += 1;
                Debug.Log($"Round {_curFinishCount} Finished ");
                SceneAnalyzer.Instance.RegisterAllEntities();
                _ = MoveToNextGrabbable();
            };
            _ = MoveToNextGrabbable();
        }

    }
}
