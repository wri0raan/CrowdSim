using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

using Zerah.CrowdSim;

public class CrowdStartUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField populationTMP;
    [SerializeField] private TMP_InputField seedTMP;
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnReset;
    [SerializeField] private GameObject panelRoot;

    [Header("Spawner")]
    [SerializeField] private CrowdSpawner spawner;

    [Header("Options")]
    [Tooltip("Start 누른 뒤 패널을 숨깁니다.")]
    [SerializeField] private bool hidePanelAfterStart = true;

    [Tooltip("인원수 입력이 비었을 때 사용할 기본값")]
    [SerializeField] private int defaultPopulation = 30;

    private void OnValidate()
    {

        if (!spawner)

        if (!panelRoot)
        {

            var tr = transform.Find("Panel_Control");
            if (tr) panelRoot = tr.gameObject;
        }
    }

    private void Awake()
    {

        if (btnStart) btnStart.onClick.RemoveListener(OnClickStart);
        if (btnReset) btnReset.onClick.RemoveListener(OnClickReset);
    }

    private void Start()
    {

        if (spawner) spawner.enabled = false;

        if (btnStart) btnStart.onClick.AddListener(OnClickStart);
        if (btnReset) btnReset.onClick.AddListener(OnClickReset);
        if (!panelRoot) panelRoot = gameObject;
    }

    private void OnClickStart()
    {
        if (!spawner)
        {
            Debug.LogError("[CrowdStartUI] Spawner가 비어있습니다. CrowdSystem의 CrowdSpawner를 드래그해 연결하세요.");
            return;
        }

        int pop = defaultPopulation;
        if (populationTMP && int.TryParse(populationTMP.text, out var v))
            pop = Mathf.Max(0, v);

        if (seedTMP && int.TryParse(seedTMP.text, out var seed))
            Random.InitState(seed);

        spawner.nearCount = pop;
        spawner.farCount = 0;
        spawner.enabled = true;

        if (hidePanelAfterStart && panelRoot) panelRoot.SetActive(false);
    }

    private void OnClickReset()
    {

        var s = SceneManager.GetActiveScene();
        SceneManager.LoadScene(s.buildIndex);
    }
}
