using BNG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneAnalyzer : MonoBehaviour
{
    /// <summary>
    /// ͨ���ű����ص���Կɽ���������з��࣬�˴���¼��ץȡ����Ĺ��ؽű�������
    /// </summary>
    public List<string> targetGrabTypeFilter = new List<string>();

    public List<GameObject> grabbableObjects = new List<GameObject>();

    /// <summary>
    /// �����е���������
    /// </summary>
    GameObject[] allGameObjects;

    /// <summary>
    /// ���ҳ����й�����ָ���ű���������Ϸ����
    /// </summary>
    /// <param name="scriptName">�ű�����</param>
    /// <returns>���й�����ָ���ű�����Ϸ�����б�</returns>
    private List<GameObject> FindObjectsWithScript(string scriptName)
    {
        List<GameObject> result = new List<GameObject>();

        // �������ж��󣬼��������Ƿ����ָ���ű�
        foreach(GameObject obj in allGameObjects)
        {
            // ��ȡ�������ϵ��������
            Component[] components = obj.GetComponents<Component>();

            foreach(Component component in components)
            {
                if(component != null && component.GetType().Name == scriptName)
                {
                    result.Add(obj);
                    break; // ���ҵ�Ŀ��ű���������ǰ���������������
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
                obj.AddComponent<Grabbable>();
            }
        }

    }

    /// <summary>
    /// �������
    /// </summary>
    private void Start()
    {
        targetGrabTypeFilter.Add("XRGrabInteractable");
        targetGrabTypeFilter.Add("Grabbable");

        // ��������
        AnalyzeScene();
    }
}
