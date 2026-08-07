using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

// 2026.08.07_psb수정
public class PrototypeGameSessionPersistenceTests
{
    private string temporarySaveDirectory;
    private Type saveStoreType;
    private PropertyInfo saveDirectoryOverride;

    [SetUp]
    public void SetUp()
    {
        saveStoreType = FindType("UserSaveFileStore");
        Assert.That(saveStoreType, Is.Not.Null);

        saveDirectoryOverride = saveStoreType.GetProperty(
            "SaveDirectoryOverrideForTests",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.That(saveDirectoryOverride, Is.Not.Null);

        temporarySaveDirectory = Path.Combine(
            Path.GetTempPath(),
            "GrimoireSaveTests",
            Guid.NewGuid().ToString("N"));

        saveDirectoryOverride.SetValue(null, temporarySaveDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (saveDirectoryOverride != null)
            saveDirectoryOverride.SetValue(null, null);

        if (!string.IsNullOrWhiteSpace(temporarySaveDirectory) &&
            Directory.Exists(temporarySaveDirectory))
        {
            Directory.Delete(temporarySaveDirectory, true);
        }
    }

    [Test]
    public void StartNewGame_CreatesJsonSaveMarkerThatSurvivesSessionReset()
    {
        Type sessionType = FindType("PrototypeGameSession");

        Assert.That(sessionType, Is.Not.Null);

        MethodInfo resetAll = sessionType.GetMethod("ResetAll");
        MethodInfo startNewGame = sessionType.GetMethod("StartNewGame");
        PropertyInfo hasSaveData = sessionType.GetProperty("HasSaveData");

        resetAll.Invoke(null, null);
        startNewGame.Invoke(null, null);

        string saveFilePath = Path.Combine(
            temporarySaveDirectory,
            "user-save.json");

        Assert.That(File.Exists(saveFilePath), Is.True);

        resetAll.Invoke(null, null);

        Assert.That((bool)hasSaveData.GetValue(null), Is.True);
    }

    private static Type FindType(string typeName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType(typeName))
            .FirstOrDefault(type => type != null);
    }
}
