using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private Material material_;
    private Color defaultColor_;
    private bool isHitEffectProcessing_;
    private float hitEffectElapsedTime_;
    
    void Start()
    {
        material_ = GetComponentInChildren<MeshRenderer>().material;
        defaultColor_ = material_.color;
        isHitEffectProcessing_ = false;
    }

    void Update()
    {
        if (isHitEffectProcessing_)
        {
            hitEffectElapsedTime_ += Time.deltaTime;
            if (hitEffectElapsedTime_ >= 0.1f)
            {
                material_.color = defaultColor_;
                isHitEffectProcessing_ = false;
            }
        }
    }

    public void Hit()
    {
        material_.color = Color.red;
        isHitEffectProcessing_ = true;
        hitEffectElapsedTime_ = 0.0f;
    }
}
