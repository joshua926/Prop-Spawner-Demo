using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Windows;
using System;

namespace PropSpawner
{
    public class RulesDetailsTile : VisualElement
    {
        public RulesDetailsTile(Rules rules)
        {
            styleSheets.Add(Resources.Load<StyleSheet>(UIStrings.rulesDetailsUSSPath));
            AddToClassList(UIStrings.rulesDetailsTileClass);
            Resources.Load<VisualTreeAsset>(UIStrings.rulesDetailsUXMLPath).CloneTree(this);

            ObjectField prefabField = this.Q<ObjectField>();
            prefabField.objectType = typeof(GameObject);
            prefabField.allowSceneObjects = false;
            this.Bind(new SerializedObject(rules));

            // Register to read back float values from ScriptableObject asset after OnValidate is called on it
            this.Query<FloatField>(className: UIStrings.radiusFieldClass).ForEach(field =>
            {
                field.RegisterCallback<ChangeEvent<float>>(e =>
                {
                    if (field.bindingPath == null) { return; }
                    var property = new SerializedObject(rules).FindProperty(field.bindingPath);
                    field.value = property.floatValue;
                    return;
                });
            });
            this.Query<Vector2Field>(className: UIStrings.zoneSizeField).ForEach(field =>
            {
                field.RegisterCallback<ChangeEvent<Vector2>>(e =>
                {
                    if (field.bindingPath == null) { return; }
                    var property = new SerializedObject(rules).FindProperty(field.bindingPath);
                    field.value = property.vector2Value;
                });
            });
            this.Query<Vector3Field>(className: UIStrings.rotSclField).ForEach(field =>
            {
                field.RegisterCallback<ChangeEvent<Vector3>>(e =>
                {
                    if (field.bindingPath == null) { return; }
                    var property = new SerializedObject(rules).FindProperty(field.bindingPath);
                    field.value = property.vector3Value;
                });
            });

            this.Query<Toggle>(className: UIStrings.useToggleClass).ForEach(toggle =>
            {
                toggle.RegisterCallback<ChangeEvent<bool>>(e =>
                {
                    SetDisplayOfRenderingOptions();
                });
            });

            SetDisplayOfRenderingOptions();

            this.Query<Button>(className: UIStrings.rulesDetailsDeleteButtonClassName).ForEach(button =>
            {
                button.clicked += () =>
                {
                    if (EditorUtility.DisplayDialog("Delete Selected Asset", "   " + AssetDatabase.GetAssetPath(rules) + Environment.NewLine + "You cannot undo this action.", "Delete", "Cancel"))
                    {
                        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(rules));
                    }
                };
            });
        }

        void SetDisplayOfRenderingOptions()
        {
            Toggle useECSRenderingToggle = this.Q<Toggle>(className: UIStrings.useECSRenderingToggleClass);
            VisualElement keepGameObjectsSection = this.Q(className: UIStrings.keepGameObjectsToggleSectionClass);
            Toggle keepGameObjectsToggle = this.Q<Toggle>(className: UIStrings.keepGameObjectsToggleClass);
            VisualElement zoneSizeSection = this.Q(className: UIStrings.zoneSizeSection);

            keepGameObjectsSection.style.display =
                useECSRenderingToggle.value ?
                DisplayStyle.Flex :
                DisplayStyle.None;
            zoneSizeSection.style.display =
                useECSRenderingToggle.value && !keepGameObjectsToggle.value ?
                DisplayStyle.Flex :
                DisplayStyle.None;
        }
    }
}