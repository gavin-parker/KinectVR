using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*  Created by Gavin Parker 03/2017
 *  Highlight a given object in green when hand is placed over it, to show the player it can be grabbed
 * 
 */
public abstract class Highlighter : MonoBehaviour
{

    protected List<Renderer> ChildMaterials;
    protected Shader[] OriginalMaterials;
    protected bool AllowOutline = true;
    private Shader _outlineShader;

    public void SetGrabbable(bool grabbable)
    {
        AllowOutline = grabbable;
    }

    public void Init()
    {
        _outlineShader = Shader.Find("Custom/Outline");
        ChildMaterials = new List<Renderer>(GetComponentsInChildren<Renderer>(false));
        ParticleSystemRenderer[] ps = GetComponentsInChildren<ParticleSystemRenderer>(false);
        foreach (ParticleSystemRenderer p in ps)
        {
            ChildMaterials.Remove(p);
        }
        List<Renderer> newChildMaterials = new List<Renderer>();
        foreach (Renderer r in ChildMaterials)
        {

            newChildMaterials.Add(r);
        }
        ChildMaterials = newChildMaterials;
        OriginalMaterials = new Shader[ChildMaterials.Count];
        for (int i = 0; i < ChildMaterials.Count; i++)
        {
            OriginalMaterials[i] = ChildMaterials[i].material.shader;
            
        }
    }

    public void SetOutline(Color color)
    {

        if (_outlineShader == null)
        {
            Init();
        }
        if (!AllowOutline)
        {
            return;
        }
        for (int i = 0; i < ChildMaterials.Count; i++)
        {
            try
            {
                if (ChildMaterials[i] != null && ChildMaterials[i].material.shader != null)
                {
                    ChildMaterials[i].material.shader = _outlineShader;
                    ChildMaterials[i].material.SetColor("Color", color);
                    ChildMaterials[i].material.SetFloat("_Outline", 0.01f);

                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    public void RemoveOutline()
    {
        if (_outlineShader == null)
        {
            Init();
        }
        for (var i = 0; i < ChildMaterials.Count; i++)
        {
            if (ChildMaterials != null && ChildMaterials[i] != null && OriginalMaterials[i] != null)
            {
                ChildMaterials[i].material.shader = OriginalMaterials[i];
            }

        }
    }

}