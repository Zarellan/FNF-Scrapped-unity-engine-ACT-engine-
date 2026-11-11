using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

[AddComponentMenu("UI/Cutout Mask", 15)]
public class CutOutMask : Image
{
    private Material _cutoutMaterial;

    public override Material materialForRendering
    {
        get
        {
            if (_cutoutMaterial == null)
            {
                _cutoutMaterial = new Material(base.materialForRendering);
                _cutoutMaterial.SetInt("_StencilComp", (int)CompareFunction.NotEqual);
            }
            return _cutoutMaterial;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_cutoutMaterial != null)
        {
            DestroyImmediate(_cutoutMaterial);
        }
    }}
