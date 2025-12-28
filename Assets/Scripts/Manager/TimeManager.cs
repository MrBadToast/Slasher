using ReadOnlyDrawer;
using System.Collections;
using UnityEngine;

public class TimeManager : StaticMonoBehaviour<TimeManager>
{
    [ReadOnly, SerializeField] private float debug_timeScale = 1f;
    [ReadOnly, SerializeField] private int pauseStack = 0;

    private void Update()
    {
        debug_timeScale = Time.timeScale;
    }

    public void ChangeTimeScale(float newTimeScale, float duration = 0f)
    {
        if (pauseStack > 0) return;

        if (duration > 0)
        {
            StopAllCoroutines();
            StartCoroutine(ChangeTimescaleTemp(newTimeScale, duration));
            return;
        }
        else
        {
            Time.timeScale = newTimeScale;
            Time.fixedDeltaTime = 0.02f * newTimeScale;
        }
    }

    private IEnumerator ChangeTimescaleTemp(float newTimeScale, float duration)
    {
        Time.timeScale = newTimeScale;
        Time.fixedDeltaTime = 0.02f * newTimeScale;

        yield return new WaitForSecondsRealtime(duration);

        if (pauseStack > 0)
        {
            yield break;
        }
        else
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }

    public void PauseTime()
    {
        Time.timeScale = 0f;

        pauseStack++;
    }

    public void UnpauseTime()
    {
        Debug.Log("UNPAUSE TIME CALLED. CURRENT PAUSE STACK: " + pauseStack);

        if (Time.timeScale != 0f || pauseStack == 0f)
            return;

        pauseStack--;

        if (pauseStack == 0)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

    }

    private void OnDisable()
    {
        ChangeTimeScale(1f);
    }
}   