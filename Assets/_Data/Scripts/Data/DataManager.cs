using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class DataManager : Singleton<DataManager>
{
    private QuestionsData _questionsData;
    private SaveData _saveData;
    private FacultyData _facultyData;
    private Dictionary<string, List<int>> _usedQuestionIds = new Dictionary<string, List<int>>();

    private string SaveDataPath => Path.Combine(Application.streamingAssetsPath, "SaveData.json");
    private string FacultyDataPath => Path.Combine(Application.streamingAssetsPath, "FacultyData.json");

    protected override void Awake()
    {
        base.Awake();
        LoadQuestions();
        LoadSaveData();
        LoadFacultyData();
    }

    private void LoadQuestions()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "QuestionsData.json");
        string json = File.ReadAllText(path);
        _questionsData = JsonConvert.DeserializeObject<QuestionsData>(json);
    }

    private void LoadSaveData()
    {
        if (File.Exists(SaveDataPath))
        {
            string json = File.ReadAllText(SaveDataPath);
            _saveData = JsonConvert.DeserializeObject<SaveData>(json);
        }
        else
        {
            _saveData = new SaveData();
        }
    }

    private void LoadFacultyData()
    {
        if (!File.Exists(FacultyDataPath))
        {
            _facultyData = new FacultyData { faculties = new List<FacultyInfo>() };
            Debug.LogWarning("FacultyData.json not found in StreamingAssets.");
            return;
        }

        string json = File.ReadAllText(FacultyDataPath);
        _facultyData = JsonConvert.DeserializeObject<FacultyData>(json);
        if (_facultyData?.faculties == null)
            _facultyData = new FacultyData { faculties = new List<FacultyInfo>() };
    }

    public Question GetRandomQuestion(string majorId)
    {
        var set = _questionsData.questions.FirstOrDefault(q => q.majorId == majorId);
        if (set == null || set.questions.Count == 0) return null;

        if (!_usedQuestionIds.ContainsKey(majorId))
            _usedQuestionIds[majorId] = new List<int>();

        var available = set.questions.Where(q => !_usedQuestionIds[majorId].Contains(q.id)).ToList();
        if (available.Count == 0)
        {
            _usedQuestionIds[majorId].Clear();
            available = new List<Question>(set.questions);
        }

        var question = available[UnityEngine.Random.Range(0, available.Count)];
        _usedQuestionIds[majorId].Add(question.id);
        return question;
    }

    public void SavePlayerProgress(string facultyId, string majorId, float percentage)
    {
        if (_saveData.MajorScores == null)
            _saveData.MajorScores = new Dictionary<string, float>();
        if (_saveData.FacultyScores == null)
            _saveData.FacultyScores = new Dictionary<string, Dictionary<string, float>>();

        if (!string.IsNullOrEmpty(majorId))
        {
            if (_saveData.MajorScores.TryGetValue(majorId, out float currentBest))
                _saveData.MajorScores[majorId] = Mathf.Max(currentBest, percentage);
            else
                _saveData.MajorScores[majorId] = percentage;
        }

        if (!string.IsNullOrEmpty(facultyId) && !string.IsNullOrEmpty(majorId))
        {
            if (!_saveData.FacultyScores.TryGetValue(facultyId, out var majorScores) || majorScores == null)
            {
                majorScores = new Dictionary<string, float>();
                _saveData.FacultyScores[facultyId] = majorScores;
            }

            if (majorScores.TryGetValue(majorId, out float currentFacultyBest))
                majorScores[majorId] = Mathf.Max(currentFacultyBest, percentage);
            else
                majorScores[majorId] = percentage;
        }

        switch (majorId)
        {
            case "GameProgramming":
                _saveData.GameProgramming = Mathf.Max(_saveData.GameProgramming, percentage);
                break;
            case "WebProgramming":
                _saveData.WebProgramming = Mathf.Max(_saveData.WebProgramming, percentage);
                break;
        }

        string json = JsonConvert.SerializeObject(_saveData, Formatting.Indented);
        File.WriteAllText(SaveDataPath, json);
    }

    public SaveData GetSaveData() => _saveData;

    public float GetBestScore(string facultyId, string majorId)
    {
        if (_saveData == null) return 0f;

        if (!string.IsNullOrEmpty(facultyId) && _saveData.FacultyScores != null
            && _saveData.FacultyScores.TryGetValue(facultyId, out var majors)
            && majors != null && majors.TryGetValue(majorId, out float facultyScore))
        {
            return facultyScore;
        }

        if (_saveData.MajorScores != null && _saveData.MajorScores.TryGetValue(majorId, out float majorScore))
            return majorScore;

        return 0f;
    }

    public FacultyInfo GetFaculty(string facultyId)
    {
        return _facultyData?.faculties?.FirstOrDefault(f => f.id == facultyId);
    }

    public List<FacultyInfo> GetFaculties()
    {
        return _facultyData?.faculties ?? new List<FacultyInfo>();
    }

    public List<MajorInfo> GetMajorsForFaculty(string facultyId)
    {
        var faculty = GetFaculty(facultyId);
        return faculty?.majors ?? new List<MajorInfo>();
    }

    public void ResetUsedQuestions() => _usedQuestionIds.Clear();
}
