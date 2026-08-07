using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// 2026.08.07_psb수정
public class PrototypeGameSessionPersistenceTests
{
    private const string HasSaveDataKey = "PrototypeGameSession.HasSaveData";

    private bool hadSaveDataKey;
    private int previousSaveDataValue;

    [SetUp]
    public void SetUp()
    {
        hadSaveDataKey = PlayerPrefs.HasKey(HasSaveDataKey);
        previousSaveDataValue = PlayerPrefs.GetInt(HasSaveDataKey);
        PlayerPrefs.DeleteKey(HasSaveDataKey);
    }

    [TearDown]
    public void TearDown()
    {
        if (hadSaveDataKey)
            PlayerPrefs.SetInt(HasSaveDataKey, previousSaveDataValue);
        else
            PlayerPrefs.DeleteKey(HasSaveDataKey);

        PlayerPrefs.Save();
    }

    [Test]
    public void ResetAll_PreservesSaveAvailabilityCreatedByNewGame()
    {
        Type sessionType = AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(assembly => assembly.GetType("PrototypeGameSession"))
            .FirstOrDefault(type => type != null);

        Assert.That(sessionType, Is.Not.Null);

        MethodInfo resetAll = sessionType.GetMethod("ResetAll");
        MethodInfo startNewGame = sessionType.GetMethod("StartNewGame");
        PropertyInfo hasSaveData = sessionType.GetProperty("HasSaveData");

        resetAll.Invoke(null, null);
        startNewGame.Invoke(null, null);

        resetAll.Invoke(null, null);

        Assert.That((bool)hasSaveData.GetValue(null), Is.True);
    }

}
