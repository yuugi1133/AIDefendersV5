using UnityEngine;

public class StageReport
{
    public bool cleared;
    public int score;
    public int enemiesKilled;
    public int friendliesPassed;
    public int enemiesPassed;
    public int friendliesKilled;
    public int identifyCorrect;
    public int identifyWrong;
    public int identifyMissed;
    public string grade;
    public int researchPointsGained;
    public int researchPointsTotal;

    public int IdentifyEvaluated
    {
        get { return identifyCorrect + identifyWrong + identifyMissed; }
    }

    public float IdentifyAccuracy
    {
        get
        {
            int count = IdentifyEvaluated;
            if (count <= 0)
                return 0f;
            return identifyCorrect / (float)count;
        }
    }

    public string IdentifyAccuracyLabel
    {
        get
        {
            int count = IdentifyEvaluated;
            if (count <= 0)
                return "—";

            int percent = Mathf.Max(0, (int)System.Math.Round(
                IdentifyAccuracy * 100f,
                System.MidpointRounding.AwayFromZero));
            return percent + "%";
        }
    }

    public string IdentifyAccuracyDetail
    {
        get
        {
            int count = IdentifyEvaluated;
            if (count <= 0)
                return "평가할 표적이 없습니다";

            return "정답 " + identifyCorrect
                + "  ·  오식별 " + identifyWrong
                + "  ·  미식별 " + identifyMissed;
        }
    }

    public static string ComputeGrade(StageReport report)
    {
        if (report == null || !report.cleared)
            return "실패";

        int count = report.IdentifyEvaluated;
        float accuracy = report.IdentifyAccuracy;
        if (count <= 0)
            return "C";

        if (accuracy >= 0.9f && report.friendliesKilled == 0 && report.enemiesPassed == 0)
            return "S";
        if (accuracy >= 0.8f && report.friendliesKilled == 0)
            return "A";
        if (accuracy >= 0.65f)
            return "B";
        if (accuracy >= 0.45f)
            return "C";
        return "D";
    }
}
