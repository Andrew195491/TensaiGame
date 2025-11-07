#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Tile_U))]
public class Tile_UEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Tile_U tile = (Tile_U)target;

        // Dibujar el campo tipo normalmente
        EditorGUILayout.PropertyField(serializedObject.FindProperty("tipo"));

        // Fase Final: Solo mostrar categoría si el tipo es Pregunta
        if (tile.tipo == Tile_U.TipoCasilla.Pregunta)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_categoria"), 
                new GUIContent("Categoría", "Categoría de pregunta asociada"));
        }

        serializedObject.ApplyModifiedProperties();

        // Mostrar descripción actual
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(tile.ObtenerDescripcion(), MessageType.Info);
    }
}
#endif