using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Tile_U))]
public class Tile_UEditor : Editor
{
    SerializedProperty tipoProp;
    SerializedProperty categoriaProp;

    void OnEnable()
    {
        tipoProp = serializedObject.FindProperty("tipo");
        categoriaProp = serializedObject.FindProperty("categoria");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Tipo de casilla", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(tipoProp);

        // Bloquea 'Categoria' si NO es Pregunta
        bool esPregunta = (Tile_U.TipoCasilla)tipoProp.enumValueIndex == Tile_U.TipoCasilla.Pregunta;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Solo si es Pregunta", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(!esPregunta))
        {
            EditorGUILayout.PropertyField(categoriaProp, new GUIContent("Categoria"));
        }

        if (!esPregunta)
        {
            EditorGUILayout.HelpBox("La categoría solo aplica cuando el tipo es 'Pregunta'.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
