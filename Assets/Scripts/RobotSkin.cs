using System;
using Mirror;
using UnityEngine;

public class RobotSkin : MonoBehaviour
{
    public int skinIndex;
    public Material[] skins;
    public MeshRenderer[] renderers;

    private void Start()
    {
        SetSkin(skinIndex);
    }

    [ContextMenu("Set Skin")]   
    public void SetSkin(int newIndex)
    {
        newIndex = Robot.Mod(newIndex, skins.Length);
        Debug.Log($"Skin set to {newIndex}");
        foreach (var meshRenderer in renderers)
        {
            var sharedMaterials = meshRenderer.sharedMaterials;
            Debug.Log(sharedMaterials.Length);
            for (var i = 0; i < meshRenderer.sharedMaterials.Length; i++)
            {
                var material = sharedMaterials[i];
                if (skins[skinIndex] == material)
                {
                    sharedMaterials[i] = skins[newIndex];
                }
            }

            meshRenderer.sharedMaterials = sharedMaterials;
        }
        skinIndex = newIndex;
    }
}
