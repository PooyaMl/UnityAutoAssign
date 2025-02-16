using AutoAssign;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AutoAssignEditor
{
    [InitializeOnLoad]
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class AutoAssignEditor : Editor
    {
        private class FieldData
        {
            public FieldInfo Field;
            public AssignTarget SourceType;

            public FieldData(FieldInfo field, AssignTarget sourceType)
            {
                Field = field;
                SourceType = sourceType;
            }
        }

        private readonly List<FieldData> autoAssignFields = new();

        static AutoAssignEditor()
        {
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        private static void OnHierarchyChanged()
        {
            foreach (var obj in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (obj == null) continue;

                var editor = CreateEditor(obj) as AutoAssignEditor;
                if (editor != null)
                {
                    editor.UpdateAutoAssignFields();
                }
            }
        }

        private void OnEnable()
        {
            if (target == null) return;

            CacheAutoAssignFields();
            UpdateAutoAssignFields();
        }

        private void CacheAutoAssignFields()
        {
            autoAssignFields.Clear();
            Type targetType = target.GetType();
            FieldInfo[] fields = targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                var attribute = field.GetCustomAttribute<AutoAssignAttribute>();
                if (attribute != null)
                {
                    if (field.IsPublic)
                    {
                        Debug.LogError($"[AutoAssign] cannot be used on public fields! " +
                                       $"Field '{field.Name}' in '{targetType.Name}' must be private (add [SerializeField] if needed).");
                        continue;
                    }

                    if (!typeof(Component).IsAssignableFrom(field.FieldType))
                    {
                        Debug.LogError($"[AutoAssign] can only be used on Component types! " +
                                       $"Field '{field.Name}' in '{targetType.Name}' is not a Component.");
                        continue;
                    }

                    autoAssignFields.Add(new FieldData(field, attribute.SourceType));
                }
            }
        }

        private void UpdateAutoAssignFields()
        {
            if (target == null) return;

            bool updated = false;

            foreach (var fieldData in autoAssignFields)
            {
                object currentValue = fieldData.Field.GetValue(target);
                object newValue = FindComponentBySourceType(fieldData);

                if (newValue != currentValue)
                {
                    fieldData.Field.SetValue(target, newValue);
                    updated = true;
                }
            }

            if (updated)
            {
                EditorUtility.SetDirty(target);
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            foreach (var fieldData in autoAssignFields)
            {
                object fieldValue = fieldData.Field.GetValue(target);

                GUI.enabled = false;
                EditorGUILayout.ObjectField(fieldData.Field.Name, (Component)fieldValue, fieldData.Field.FieldType, true);
                GUI.enabled = true;
            }
        }

        private Component FindComponentBySourceType(FieldData fieldData)
        {
            MonoBehaviour monoTarget = (MonoBehaviour)target;
            AssignTarget sourceType = fieldData.SourceType;
            Type componentType = fieldData.Field.FieldType;

            switch (sourceType)
            {
                case AssignTarget.Self:
                    return monoTarget.GetComponent(componentType);

                case AssignTarget.Child:
                    return monoTarget.GetComponentInChildren(componentType, true);

                case AssignTarget.Parent:
                    return monoTarget.GetComponentInParent(componentType, true);

                case AssignTarget.Any:
                    return FindFirstObjectByType(componentType) as Component;

                default:
                    Debug.LogWarning($"Unknown sourceType '{sourceType}' in {monoTarget.name}");
                    return null;
            }
        }
    }
}