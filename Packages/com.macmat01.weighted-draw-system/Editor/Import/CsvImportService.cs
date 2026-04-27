using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Authoring;
using UnityEditor;
using UnityEngine;
namespace Editor.Import
{
    public static class CsvImportService
    {
        public static void CompileGeneric<T>(CsvDataSourceSO<T> authoring) where T : class
        {
            if (authoring == null)
            {
                return;
            }

            IRowDeserializer<T> deserializer = GetDeserializer(authoring);
            if (deserializer == null)
            {
                Debug.LogError($"CsvImportService: Could not obtain IRowDeserializer<{typeof(T).Name}> from {authoring.GetType().Name}.");
                return;
            }

            CompiledCsvTableSO<T> compiledTable = EnsureCompiledTable(authoring);
            if (compiledTable == null)
            {
                return;
            }

            IReadOnlyList<TextAsset> sourceFiles = authoring.SourceCsvFiles;
            List<string> csvContents = (from source in sourceFiles where source != null select source.text).ToList();

            CsvRowCompiler<T> compiler = new CsvRowCompiler<T>(null, deserializer);
            List<T> rows = compiler.Compile(csvContents, authoring.Columns);
            compiledTable.SetRows(rows);
            EditorUtility.SetDirty(compiledTable);
            EditorUtility.SetDirty(authoring);
            AssetDatabase.SaveAssets();
        }

        private static IRowDeserializer<T> GetDeserializer<T>(CsvDataSourceSO<T> authoring) where T : class
        {
            MethodInfo getDeserializerMethod = authoring.GetType().GetMethod("GetDeserializer", BindingFlags.NonPublic | BindingFlags.Instance);
            if (getDeserializerMethod == null)
            {
                return null;
            }

            return getDeserializerMethod.Invoke(authoring, Array.Empty<object>()) as IRowDeserializer<T>;
        }

        private static CompiledCsvTableSO<T> EnsureCompiledTable<T>(CsvDataSourceSO<T> authoring) where T : class
        {
            PropertyInfo compiledTableProperty = authoring.GetType().GetProperty("CompiledTable", BindingFlags.Public | BindingFlags.Instance);
            if (compiledTableProperty == null)
            {
                return null;
            }

            CompiledCsvTableSO<T> existingTable = compiledTableProperty.GetValue(authoring) as CompiledCsvTableSO<T>;
            if (existingTable != null)
            {
                return existingTable;
            }

            CompiledCsvTableSO<T> newTable = ScriptableObject.CreateInstance<CompiledCsvTableSO<T>>();
            if (newTable == null)
            {
                Type closedTableType = typeof(CompiledCsvTableSO<>).MakeGenericType(typeof(T));
                newTable = ScriptableObject.CreateInstance(closedTableType) as CompiledCsvTableSO<T>;
            }

            if (newTable == null)
            {
                Type concreteTableType = FindConcreteCompiledTableType<T>();
                if (concreteTableType != null)
                {
                    newTable = ScriptableObject.CreateInstance(concreteTableType) as CompiledCsvTableSO<T>;
                }
            }

            if (newTable == null)
            {
                Debug.LogError($"CsvImportService: Could not create {typeof(CompiledCsvTableSO<>).Name}<{typeof(T).FullName}>.");
                return null;
            }

            newTable.name = "CompiledData";
            if (AssetDatabase.Contains(authoring))
            {
                AssetDatabase.AddObjectToAsset(newTable, authoring);
            }

            PropertyInfo setPropertyInfo = authoring.GetType().GetProperty("CompiledTable", BindingFlags.Public | BindingFlags.Instance);
            if (setPropertyInfo?.CanWrite == true)
            {
                setPropertyInfo.SetValue(authoring, newTable);
            }

            return newTable;
        }

        private static Type FindConcreteCompiledTableType<T>() where T : class
        {
            Type closedTableType = typeof(CompiledCsvTableSO<>).MakeGenericType(typeof(T));
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(SafeGetTypes)
                .FirstOrDefault(type =>
                    type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } &&
                    closedTableType.IsAssignableFrom(type));
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            if (assembly == null)
            {
                return Array.Empty<Type>();
            }

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(static type => type != null);
            }
        }
    }
}
