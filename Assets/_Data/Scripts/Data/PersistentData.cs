public class PersistentData : Singleton<PersistentData>
{
    public string CurrentFaculty { get; set; }
    public string CurrentMajor { get; set; }
    public int JumpScore { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }

    protected override void Awake()
    {
        base.Awake();
        ResetSession();
    }

    public void ResetSession()
    {
        JumpScore = 0;
        CorrectAnswers = 0;
        TotalQuestions = 0;
        if (string.IsNullOrEmpty(CurrentFaculty))
            CurrentFaculty = "IT";
        if (string.IsNullOrEmpty(CurrentMajor))
            CurrentMajor = "GameProgramming";
    }

    public float CalculatePercentage()
    {
        if (TotalQuestions == 0) return 0f;
        return (float)CorrectAnswers / TotalQuestions * 100f;
    }
}
