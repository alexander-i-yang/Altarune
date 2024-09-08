using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class BubbleSnap : MonoBehaviour
{
    private ParticleSystem p_system;
    private ParticleSystem.Particle[] p_particles;
    [SerializeField] private int precision;
    

    private void Awake()
    {
        intitializeIfNeeded();
    }

    private List<Vector2> subpixels = new List<Vector2>();
    
    private void FixedUpdate()
    {
        int numParticles = p_system.GetParticles(p_particles);
        if (subpixels.Count < numParticles)
        {
            for (int i = subpixels.Count; i < numParticles; ++i)
            {
                subpixels.Add(Vector2.zero);
            }
        }

        for (int i = 0; i < numParticles; i++)
        {
            Vector2 p = (Vector2)(p_particles[i].position) + subpixels[i];
            Vector2 truncP = new Vector2(TruncateValue(p.x), TruncateValue(p.y));
            Vector2 subP = p - truncP;
            subpixels[i] = subP;
            p_particles[i].position = new Vector3(truncP.x, truncP.y, 1);
        }
        p_system.SetParticles(p_particles, numParticles);
    }

    void intitializeIfNeeded()
    {
        if (p_system == null)
            p_system = gameObject.GetComponent<ParticleSystem>();
        if (p_particles == null || p_particles.Length < p_system.main.maxParticles)
            p_particles = new ParticleSystem.Particle[p_system.main.maxParticles];
    }

    float TruncateValue(float val)
    {
        return (int)(val * precision) / precision;
    }
}
