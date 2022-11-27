// See https://aka.ms/new-console-template for more information

using PatientApp;

var obj = new PatientAssessment()
{
    MedicalHistory = "First text"
};

var obj2 = new PatientAssessment()
{
    MedicalHistory = "second text"
};

obj.CompareAndMergeWith(obj2);

Console.WriteLine(obj.MedicalHistory);