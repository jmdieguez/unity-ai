using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;

public class BasketballSettings : MonoBehaviour
{
    public int totalScore;
    public int totalEpisodes;

    float shotConversion;
    public Text scoreText;

    StatsRecorder m_Recorder;

    public void Awake()
    {
        m_Recorder = Academy.Instance.StatsRecorder;
    }

    public void Update()
    {
        scoreText.text = $"Score: {totalScore}";

        // Send stats via SideChannel so that they'll appear in TensorBoard.
        // These values get averaged every summary_frequency steps, so we don't
        // need to send every Update() call.
        if ((Time.frameCount % 100) == 0)
        {
            shotConversion = (float)totalScore / totalEpisodes;
            m_Recorder.Add("ShotConversion", shotConversion, StatAggregationMethod.MostRecent);

            // Every 100k episodes we reset everything
            if (totalEpisodes > 100000) {
                totalScore = 0;
                totalEpisodes = 0;
            }
        }
    }
}
