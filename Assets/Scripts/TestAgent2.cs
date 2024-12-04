using BNG;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class TestAgent2 : BaseAgent
{

    /// <summary>
    /// ��ȡ����Ŀ�ץȡ����
    /// </summary>
    protected override void GetNextGrabbable(out Grabbable nextGrabbable)
    {
        nextGrabbable = _environmentGrabbables
            .Where(grabbable => _environmentGrabbablesState[grabbable] == false)
            .OrderBy(grabbable => Vector3.Distance(transform.position, grabbable.transform.position))
            .FirstOrDefault();
    }

}