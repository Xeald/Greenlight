using UnityEngine;
using UnityEditor;
using Greenlight.Core;

namespace Greenlight.Editor
{
    public class FlagRemovalTest
    {
        [MenuItem("Greenlight/Tests/Run Flag Removal Test")]
        public static void RunTest()
        {
            Debug.Log("[FlagRemovalTest] Starting test...");

            // Create a temporary instance
            var gameState = ScriptableObject.CreateInstance<GameStateSO>();

            // Test Bool Removal
            gameState.SetBool("TestBool", true);
            if (!gameState.HasBool("TestBool")) LogFail("Bool setup failed");
            gameState.RemoveBool("TestBool");
            if (gameState.HasBool("TestBool")) LogFail("RemoveBool failed: Key still exists");
            if (gameState.GetBool("TestBool") != false) LogFail("RemoveBool failed: Value not default");

            // Test Int Removal
            gameState.SetInt("TestInt", 42);
            if (!gameState.HasInt("TestInt")) LogFail("Int setup failed");
            gameState.RemoveInt("TestInt");
            if (gameState.HasInt("TestInt")) LogFail("RemoveInt failed: Key still exists");
            if (gameState.GetInt("TestInt") != 0) LogFail("RemoveInt failed: Value not default");

            // Test String Removal
            gameState.SetString("TestString", "Hello");
            if (!gameState.HasString("TestString")) LogFail("String setup failed");
            gameState.RemoveString("TestString");
            if (gameState.HasString("TestString")) LogFail("RemoveString failed: Key still exists");
            if (gameState.GetString("TestString") != "") LogFail("RemoveString failed: Value not default");

            Debug.Log("[FlagRemovalTest] Test passed! (If no errors above)");
        }

        private static void LogFail(string message)
        {
            Debug.LogError($"[FlagRemovalTest] FAIL: {message}");
        }
    }
}
