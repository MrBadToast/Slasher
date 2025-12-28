using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WaveManager : StaticMonoBehaviour<WaveManager>
{
    [SerializeField] private bool initiateWavesOnStart = false;
    [SerializeField] private SingleWave[] waves;
    [SerializeField] private float timeBetweenWaves = 1.0f;
    [SerializeField] private UnityEvent onAllWavesCompleted;

    [SerializeField,ReadOnly] public List<GameObject> enemies = new List<GameObject>();

    public delegate EnemyBehaviorBase OnEnemyDestroyed(EnemyBehaviorBase enemy);

    SingleWave currentWave;
    Coroutine waveProgressCoroutine;

    private void Start()
    {
        if (initiateWavesOnStart)
        {
            StartCoroutine(Cor_WaveProgress());
        }
    }

    public void StartWaves()
    {
        if (waveProgressCoroutine != null) return;

        StopAllCoroutines();
        waveProgressCoroutine = StartCoroutine(Cor_WaveProgress());
    }

    public void SpawnOneEnenmy(GameObject enemyPrefab, Transform location)
    {
        GameObject spawned = Instantiate(enemyPrefab, location.position, Quaternion.identity);
        enemies.Add(spawned);
        UI_WaveIndicator.Instance.SetEnemyCount(enemies.Count);
    }

    public void RequestRemoveFromEnemylist()
    {
        DelayedRemove().Forget();
    }

    private async UniTask DelayedRemove()
    {
        await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
        enemies.RemoveAll(e => e == null);
        UI_WaveIndicator.Instance.SetEnemyCount(enemies.Count);
    }

    IEnumerator Cor_WaveProgress()
    {
        for(int i = 0; i < waves.Length; i++)
        {
            currentWave = waves[i];

            yield return StartCoroutine(UI_WaveIndicator.Instance.Cor_ShowWaveInfo(i + 1));

            UI_WaveIndicator.Instance.ShowMonsterCount();

            foreach (WaveSegment segment in currentWave.segments)
            {
                yield return StartCoroutine(segment.ActionSegment(this));
            }

            UI_WaveIndicator.Instance.HideMonsterCount();
            yield return StartCoroutine(UI_WaveIndicator.Instance.Cor_ShowWaveClear());

            yield return new WaitForSeconds(timeBetweenWaves);
        }

        onAllWavesCompleted?.Invoke();
    }

    [System.Serializable]
    public class SingleWave
    {
        [SerializeReference] public WaveSegment[] segments;
    }
}

[System.Serializable]
public class WaveSegment
{
    public virtual IEnumerator ActionSegment(WaveManager wm) { yield return null; }
}

[System.Serializable]
public class Wait : WaveSegment
{
    public float duration;
    public override IEnumerator ActionSegment(WaveManager wm)
    {
        yield return new WaitForSeconds(duration);
    }
}

[System.Serializable]
public class SpawnEnemy : WaveSegment
{
    public Transform location;
    public GameObject enemyPrefab;
    public float spawnRate = 1.0f;

    public override IEnumerator ActionSegment(WaveManager wm)
    {
        wm.SpawnOneEnenmy(enemyPrefab, location);
        yield break;
    }
}

[System.Serializable]
public class WaitUntilClear : WaveSegment
{
    public override IEnumerator ActionSegment(WaveManager wm)
    {
        if (wm.enemies == null)
            yield break;

        yield return new WaitUntil(() => wm.enemies.Count == 0);
    }
}
