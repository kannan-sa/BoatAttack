using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public static class UtilityTools 
{
    [MenuItem("Tools/FixButton")]
    private static void FixButtons() {
        if (Selection.activeGameObject == null) {
            Debug.LogWarning("No GameObject selected. Please select a GameObject with child buttons.");
            return;
        }

        Button[] buttons = Selection.activeGameObject.GetComponentsInChildren<Button>(true);
        if (buttons.Length == 0) {
            Debug.LogWarning("No buttons found in the selected GameObject.");
            return;
        }

        int fixedCount = 0;
        foreach (Button button in buttons) {
            if (button.transition == Selectable.Transition.SpriteSwap) {
                SpriteState state = button.spriteState;
                state.highlightedSprite = button.image.sprite;
                button.spriteState = state;
                fixedCount++;
                EditorUtility.SetDirty(button);
            }
        }
        Debug.Log($"Fixed {fixedCount} buttons in {Selection.activeGameObject.name}.");
    }
}
