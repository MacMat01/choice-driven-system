using System;
using System.Reflection;
using UnityEngine;
namespace Authoring
{
    static class EditorImportBridge
    {
        public static void Compile<T>(CsvDataSourceSO<T> authoring) where T : class
        {
#if UNITY_EDITOR
            Type importerType = Type.GetType("Editor.Import.CsvImportService, MacMat01.WeightedDrawSystem.Editor");
            if (importerType == null)
            {
                Debug.LogWarning("EditorImportBridge: Could not locate 'Editor.Import.CsvImportService' in assembly 'MacMat01.WeightedDrawSystem.Editor'. CSV auto-compile was skipped.");
                return;
            }

            MethodInfo compileGenericMethod = importerType.GetMethod("CompileGeneric", BindingFlags.Public | BindingFlags.Static);
            if (compileGenericMethod == null)
            {
                Debug.LogWarning("EditorImportBridge: Found CsvImportService but missing public static method 'CompileGeneric'. CSV auto-compile was skipped.");
                return;
            }

            try
            {
                compileGenericMethod.MakeGenericMethod(typeof(T)).Invoke(null, new object[]
                {
                    authoring
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"EditorImportBridge: Failed to invoke CsvImportService.CompileGeneric<{typeof(T).Name}>. Exception: {ex}");
            }
#else
            _ = authoring;
#endif
        }
    }
}
