using System;
using System.Reflection;
namespace Authoring
{
    static class EditorImportBridge
    {
        public static void Compile<T>(CsvDataSourceSO<T> authoring) where T : class
        {
#if UNITY_EDITOR
            Type importerType = Type.GetType("Editor.Import.CsvImportService, MacMat01.WeightedDrawSystem.Editor");
            MethodInfo compileGenericMethod = importerType?.GetMethod("CompileGeneric", BindingFlags.Public | BindingFlags.Static);
            compileGenericMethod?.MakeGenericMethod(typeof(T)).Invoke(null, new object[]
            {
                authoring
            });
#else
            _ = authoring;
#endif
        }
    }
}
