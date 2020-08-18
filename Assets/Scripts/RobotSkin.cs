using System;
using Mirror;
using UnityEngine;

public class RobotSkin : MonoBehaviour
{
    public int skinIndex;
    public Material[] skins;
    public MeshRenderer[] renderers;

    [ContextMenu("Set Skin")]   
    public void SetSkin(int newIndex)
    {
        //todo: Tu pridat funkcne menenie skinov
        Debug.Log(skinIndex);
        if (newIndex < 0) newIndex = 0;
        if (newIndex > skins.Length) newIndex = skins.Length - 1;
        foreach (var meshRenderer in renderers)
        {
            var sharedMaterials = meshRenderer.sharedMaterials;
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
