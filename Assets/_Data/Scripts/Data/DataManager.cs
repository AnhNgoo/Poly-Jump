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

    public void SavePlayerProgress(string majorId, float percentage)
    {
        switch (majorId)
        {
            case "GameProgramming":
                _saveData.GameProgramming = percentage;
                break;
            case "WebProgramming":
                _saveData.WebProgramming = percentage;
                break;
        }

        string json = JsonConvert.SerializeObject(_saveData, Formatting.Indented);
        File.WriteAllText(SaveDataPath, json);
    }

    public SaveData GetSaveData() => _saveData;

    public FacultyInfo GetFaculty(string facultyId)
    {
        return _facultyData?.faculties?.FirstOrDefault(f => f.id == facultyId);
    }

    public List<MajorInfo> GetMajorsForFaculty(string facultyId)
    {
        var faculty = GetFaculty(facultyId);
        return faculty?.majors ?? new List<MajorInfo>();
    }

    public void ResetUsedQuestions() => _usedQuestionIds.Clear();
}
