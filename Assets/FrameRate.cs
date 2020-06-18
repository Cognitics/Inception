
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class LODEvent : UnityEvent<float> { }

public class FrameRate : MonoBehaviour
{
    public float FPS = 0.0f;
    public float UpdateInterval = 1.0f;
    public LODEvent OnUpdate = new LODEvent();

    private int frameCount = 0;
    private float accumulatedFPS = 0.0f;

    void Start()
    {
        StartCoroutine(UpdateCoroutine());
    }

    void Update()
    {
        accumulatedFPS += Time.timeScale / Time.deltaTime;
        ++frameCount;
    }

    IEnumerator UpdateCoroutine()
    {
        yield return new WaitForSeconds(1.0f);
        while (true)
        {
            FPS = accumulatedFPS / frameCount;
            accumulatedFPS = 0.0f;
            frameCount = 0;
            OnUpdate.Invoke(FPS);
            yield return new WaitForSeconds(UpdateInterval);
        }
    }

}
