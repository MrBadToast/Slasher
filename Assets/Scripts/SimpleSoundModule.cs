using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
public class SimpleSoundModule : MonoBehaviour
{
    [SerializeField] private SoundItem[] soundItems;
    private AudioSource aud;

    private CancellationTokenSource cts_transition = new CancellationTokenSource();

    private void Awake()
    {
        aud = GetComponent<AudioSource>();
    }

    /// <summary>
    /// 사운드를 바로 재생합니다.
    /// </summary>
    /// <param name="soundName"> 재생할 사운드 name </param>
    public void Play(string soundName)
    {
        try
        {
            var cur = SoundItem.GetSoundItem(soundItems, soundName);

            aud.clip = cur.audioClips[Random.Range(0, cur.audioClips.Length)];

            if (cur.randomPitch)
                aud.pitch = Random.Range(cur.pitchMin, cur.pitchMax);
            else
                aud.pitch = 1.0f;

            if (cur.randomVolume)
                aud.volume = Random.Range(cur.volumeMin, cur.volumeMax);
            else
                aud.volume = 1.0f;

            aud.Play();
        }
#pragma warning disable CS0168
        catch (NullReferenceException n)
        {
            Debug.LogError("SimpleSoundModule : Cannot found soundname \"" + soundName + "\" in Object \"" + gameObject.name + "\"" );
        }
        catch(IndexOutOfRangeException i)
        {
            Debug.LogError("SimpleSoundModule : soundname \"" + soundName + "\" in Object \"" + gameObject.name + "\" : Audio is Empty!");
        }
#pragma warning restore CS0168
    }

    public void Stop()
    {
        aud.Stop();
        CancelTransition();
    }

    /// <summary>
    /// 사운드를 재생하며 지정한 시간동안 볼륨을 늘리며 페이드인합니다.
    /// </summary>
    /// <param name="soundName"> 재생할 사운드 name </param>
    /// <param name="time"> 시간 (초) </param>
    public void FadeIn(string soundName, float time)
    {
        Play(soundName);

        if(cts_transition.Token == CancellationToken.None) // is there no ongoing transition
        {
            Asy_FadeIn(aud.volume, time).Forget(); // start fade in
        }
        else
        {
            if (!cts_transition.Token.IsCancellationRequested) // is ongoing transition not cancelled yet
            {
                CancelTransition(); // cancel ongoing transition
            }
            Asy_FadeIn(aud.volume, time).Forget(); // start fade in
        } 
    }

    /// <summary>
    /// 재생중인 사운드를 지정한 시간동안 볼륨을 줄여가며 페이드아웃합니다.
    /// </summary>
    /// <param name="time">시간 (초)</param>
    public void FadeOut(float time)
    {
        if (aud.volume == 0f) return;

        if(cts_transition.Token == CancellationToken.None)
        {
            Asy_FadeOut(aud.volume, time).Forget();
        }
        else
        {
            if(!cts_transition.Token.IsCancellationRequested)
            {
                CancelTransition();
            }
            Asy_FadeOut(aud.volume, time).Forget();
        }
    }

    private async UniTask Asy_FadeIn(float maxVol, float time)
    {
        try
        {
            aud.volume = 0f;
            for (float i = 0; i < 1; i += Time.deltaTime / time)
            {
                aud.volume = i * maxVol;
                await UniTask.WaitForFixedUpdate(cancellationToken: cts_transition.Token);
            }
            aud.volume = maxVol;
        }
        catch (Exception ex) when (ex is OperationCanceledException)
        {
            aud.volume = maxVol;
        }
    }

    private async UniTask Asy_FadeOut(float currentVol, float time)
    {
        try
        {
            for (float i = 1; i > 0; i -= Time.deltaTime / time)
            {
                aud.volume = i * currentVol;
                await UniTask.WaitForFixedUpdate(cancellationToken: cts_transition.Token);
            }
            aud.volume = 0f;
        }
        catch (Exception ex) when (ex is OperationCanceledException)
        {
            aud.volume = 0f;
        }
        
    }

    private void CancelTransition()
    {
        cts_transition.Cancel();
        cts_transition.Dispose();
        cts_transition = new CancellationTokenSource();
    }

    private void OnDestroy()
    {
        cts_transition.Cancel();
        cts_transition.Dispose();
    }

}

[Serializable]
public class SoundItem
{
    public string name;
    public AudioClip[] audioClips;

    public bool randomPitch;
    [Range(-3f,3f)]
    public float pitchMin = 1.0f;
    [Range(-3f,3f)]
    public float pitchMax = 1.0f;

    public bool randomVolume;
    [Range(0f, 1f)]
    public float volumeMin = 1.0f;
    [Range(0f, 1f)]
    public float volumeMax = 1.0f;

    public static SoundItem GetSoundItem(SoundItem[] soundItems, string key)
    {
        return Array.Find(soundItems, i => i.name == key);      
    }
}