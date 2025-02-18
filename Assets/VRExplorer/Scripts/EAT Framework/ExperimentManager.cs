using System;
using System.Collections;
using System.Linq;
using TsingPigSDK;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityEditor.TestTools.CodeCoverage;
namespace VRExplorer
{
    public class ExperimentManager : Singleton<ExperimentManager>
    {
        public float reportCoverageDuration = 5f;
        public event Action ExperimentFinishEvent;

        private float _timeStamp;

        /// <summary>
        /// ��ȡ�ܴ���״̬����
        /// </summary>
        /// <param name="enumType"></param>
        /// <returns></returns>
        public int TriggeredStateCount
        {
            get
            {
                int res = 0;
                foreach(var v in EntityManager.Instance.entityStates.Values)
                {
                    res += v.Count;
                }
                return res;
            }
        }

        /// <summary>
        /// ��ȡ��״̬����
        /// </summary>
        public int StateCount { get; set; }

        /// <summary>
        /// ��ȡ�ܿɽ����������
        /// </summary>
        public int InteractableCount
        {
            get { return EntityManager.Instance.monoState.Count; }
        }

        /// <summary>
        /// ��ȡ��̽�����Ŀɽ����������
        /// </summary>
        public int CoveredInteractableCount
        {
            get { return EntityManager.Instance.monoState.Count((monoPair) => { return monoPair.Value == true; }); }
        }

        public void ShowMetrics()
        {
            Debug.Log(new RichText()
                .Add("TimeCost: ").Add((Time.time - _timeStamp).ToString(), bold: true, color: Color.yellow)
                .Add(", TriggeredStateCount: ", bold: true).Add(TriggeredStateCount.ToString(), bold: true, color: Color.yellow)
                .Add(", StateCount: ", bold: true).Add(StateCount.ToString(), bold: true, color: Color.yellow)
                .Add(", CoveredInteractableCount: ", bold: true).Add(CoveredInteractableCount.ToString(), bold: true, color: Color.yellow)
                .Add(", InteractableCount: ", bold: true).Add(InteractableCount.ToString(), bold: true, color: Color.yellow)
                .Add(", Interactable Coverage: ", bold: true).Add($"{CoveredInteractableCount * 100f / InteractableCount:F2}%", bold: true, color: Color.yellow)
                .Add(", StateCount Coverage: ", bold: true).Add($"{TriggeredStateCount * 100f / StateCount:F2}%", bold: true, color: Color.yellow));
            CodeCoverage.GenerateReportWithoutStopping();
        }

        public void ExperimentFinish()
        {
            ShowMetrics();
            Debug.Log(new RichText().Add("Experiment Finished", color: Color.yellow, bold: true));
            StateCount = 0;
            ExperimentFinishEvent?.Invoke();

            StopAllCoroutines();
            CodeCoverage.StopRecording();
            UnityEditor.EditorApplication.isPlaying = false;
        }

        public void StartRecording()
        {
            _timeStamp = Time.time;
            StartCoroutine("RecordCoroutine");
        }

        private IEnumerator RecordCoroutine()
        {
            yield return null;
            ShowMetrics();
            yield return new WaitForSeconds(reportCoverageDuration);
            StartCoroutine(RecordCoroutine());
        }
    }
}