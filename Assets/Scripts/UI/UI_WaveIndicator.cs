using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;

public class UI_WaveIndicator : StaticMonoBehaviour<UI_WaveIndicator>
{
    [SerializeField] float displayDuration = 2f;

    [SerializeField] TextMeshProUGUI waveText;
    [SerializeField] TextMeshProUGUI enemyCountText;
    [SerializeField] string wavePrefix = "Wave Incomming : Wave ";
    [SerializeField] string waveClear = "Wave Cleared!";
    [SerializeField] string allWavesClear = "All Waves Cleared!";
    [SerializeField] DOTweenAnimation waveTweenAnim;
    [SerializeField] DOTweenAnimation enemyCountAnim;

    public void SetEnemyCount(int count)
    {
        enemyCountText.text = "X " +  count.ToString();
    }

    public void ShowMonsterCount()
    {
        enemyCountAnim.DORestartById("MonstersLeft_Open");
    }

    public void HideMonsterCount()
    {
        enemyCountAnim.DORestartById("MonstersLeft_Close");
    }

    public IEnumerator Cor_ShowWaveInfo(int waveNumber)
    {
        waveText.text = wavePrefix + waveNumber.ToString();
        waveTweenAnim.DORestartById("WaveUI_Open");
        yield return new WaitForSeconds(displayDuration);
        waveTweenAnim.DORestartById("WaveUI_Close");
    }

    public IEnumerator Cor_ShowWaveClear()
    {
        waveText.text = waveClear;
        waveTweenAnim.DORestartById("WaveUI_Open");
        yield return new WaitForSeconds(displayDuration);
        waveTweenAnim.DORestartById("WaveUI_Close");
    }

    public void ShowAllWaveClear()
    {
        waveText.text = allWavesClear;
        waveTweenAnim.DORestartById("WaveUI_Open");
    }
}
