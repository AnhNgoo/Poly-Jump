using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class Question
{
    public int id;
    public string question;
    public List<string> options;
    public int correctIndex;
}

[Serializable]
public class QuestionsSet
{
    public string majorId;
    public List<Question> questions;
}

[Serializable]
public class QuestionsData
{
    public List<QuestionsSet> questions;
}

[Serializable]
public class MajorInfo
{
    public string id;
    public string displayName;
}

[Serializable]
public class FacultyInfo
{
    public string id;
    public string displayName;
    public List<MajorInfo> majors;
}

[Serializable]
public class FacultyData
{
    public List<FacultyInfo> faculties;
}

[Serializable]
public class SaveData
{
    public float GameProgramming;
    public float WebProgramming;
}
