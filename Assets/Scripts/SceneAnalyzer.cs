using BNG;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using TsingPigSDK;
using Unity.VisualScripting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VRAgent
{
    public class SceneAnalyzer : TsingPigSDK.Singleton<SceneAnalyzer>
    {
        /// <summary>
        /// ͨ���ű����ص���Կɽ���������з��࣬�˴���¼��ץȡ����Ĺ��ؽű�������
        /// </summary>
        public List<string> targetGrabTypeFilter = new List<string>();


        public HashSet<GameObject> grabbableObjects = new HashSet<GameObject>();

        /// <summary>
        /// �����е���������
        /// </summary>
        private GameObject[] allGameObjects;

        /// <summary>
        /// ���ҳ����й�����ָ���ű���������Ϸ����
        /// </summary>
        /// <param name="scriptName">�ű�����</param>
        /// <returns>���й�����ָ���ű�����Ϸ�����б�</returns>
        private List<GameObject> FindObjectsWithScript(string scriptName)
        {
            List<GameObject> result = new List<GameObject>();

            foreach(GameObject obj in allGameObjects)
            {
                Component[] components = obj.GetComponents<Component>();

                foreach(Component component in components)
                {
                    if(component != null && component.GetType().Name == scriptName)
                    {
                        result.Add(obj);
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// ͨ�����ؽű����͹����������ಢ��¼��ץȡ������
        /// </summary>
        public void AnalyzeScene()
        {
            allGameObjects = FindObjectsOfType<GameObject>();

            foreach(string scriptName in targetGrabTypeFilter)
            {
                List<GameObject> objects = FindObjectsWithScript(scriptName);
                grabbableObjects.AddRange(objects);

                Debug.Log($"�ű� {scriptName} ���صĶ�������: {objects.Count}");
            }

            foreach(GameObject obj in grabbableObjects)
            {
                if(!obj.GetComponent<Grabbable>())
                {
                    var grabbable = obj.AddComponent<Grabbable>();
                    grabbable.GrabPhysics = GrabPhysics.PhysicsJoint;
                    grabbable.CollisionSpring = 10000f;
                    grabbable.CollisionSlerp = 1000f;

                    grabbable.SnapHandModel = false;
                    grabbable.ParentHandModel = false;
                    grabbable.ParentToHands = true;
                }
            }
        }

        /// <summary>
        /// �������
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            targetGrabTypeFilter.Add("XRGrabInteractable");
            targetGrabTypeFilter.Add("Grabbable");
            AnalyzeScene();
        }

        #region ָ�꣨Metrics��


        /// <summary>
        /// �洢ÿ��ʵ��Ĵ���״̬
        /// </summary>
        public Dictionary<BaseEntity, HashSet<Enum>> entityStates = new Dictionary<BaseEntity, HashSet<Enum>>();


        /// <summary>
        /// ʹ�÷��䣬ע������ʵ��
        /// </summary>
        public void RegisterAllEntities()
        {
            GetTotalStateCount = 0;
            entityStates = new Dictionary<BaseEntity, HashSet<Enum>>();

            var entityTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(BaseEntity).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach(var entityType in entityTypes)
            {

                var allEntities = FindObjectsOfType(entityType);
                foreach(var entity in allEntities)
                {
                    RegisterEntity((BaseEntity)entity);
                }
            }
        }

        /// <summary>
        /// ע��ʵ�岢��ʼ��״̬
        /// </summary>
        /// <param name="entity"></param>
        private void RegisterEntity(BaseEntity entity)
        {
            if(!entityStates.ContainsKey(entity))
            {
                entityStates[entity] = new HashSet<Enum>();
                var interfaces = entity.GetType().GetInterfaces();
                foreach(var iface in interfaces)
                {
                    var nestedTypes = iface.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                    foreach(var nestedType in nestedTypes)
                    {
                        if(nestedType.IsEnum)
                        {
                            var enumValues = Enum.GetValues(nestedType);
                            GetTotalStateCount += enumValues.Length;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ����ʵ��״̬
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="state"></param>
        public void TriggerState(BaseEntity entity, Enum state)
        {
            if(entityStates.ContainsKey(entity) && !entityStates[entity].Contains(state))
            {
                entityStates[entity].Add(state);
                Debug.Log(new RichText()
                    .Add($"Entity ", bold: true)
                    .Add(entity.Name, bold: true, color: Color.yellow)
                    .Add(" Event ", bold: true)
                    .Add(new StackTrace().GetFrame(1).GetMethod().Name, bold: true, color: Color.green)
                    .GetText());
            }
        }

        /// <summary>
        /// ��ȡ��״̬����
        /// </summary>
        /// <param name="enumType"></param>
        /// <returns></returns>
        public int GetTotalTriggeredStateCount
        {
            get
            {
                int res = 0;
                foreach(var v in entityStates.Values)
                {
                    res += v.Count;
                }
                return res;
            }
        }

        public int GetTotalStateCount { get; set; }


        public void ShowMetrics()
        {
            Debug.Log(new RichText()
                .Add("TriggeredStateCount: ", bold: true)
                .Add(GetTotalTriggeredStateCount.ToString(), bold: true, color: Color.yellow)
                .Add(", TotalStateCount: ", bold: true)
                .Add(GetTotalStateCount.ToString(), bold: true, color: Color.yellow));
        }

        #endregion ָ�꣨Metrics��


    }
}