using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerHealth : StaticMonoBehaviour<UI_PlayerHealth>
{
    [SerializeField, Range(0f, 1f)] private float sliderChangeDamp = 0.4f;
    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private Image skillRadial;
    [SerializeField] private DOTweenAnimation tweenAnim;
    [SerializeField] private DOTweenAnimation skillReadyAnim;

    float slider_value = 1f;

    private void Update()
    {
        healthBarSlider.value = Mathf.Lerp(healthBarSlider.value,slider_value,sliderChangeDamp);
    }

    protected override void Awake()
    {
        base.Awake();
    }

    public void SetHealthbarValue(float value, bool shakeEffect = false)
    {
        slider_value = value;

        if (shakeEffect)
            tweenAnim.DORestartById("Shake");
    }

    float prevFillAmount = 0f;

    public void SetSkilRadialValue(float value)
    {
        value = Mathf.Clamp01(value);

        if (prevFillAmount != 1f && value == 1f)
            skillReadyAnim.DORestart();

        skillRadial.fillAmount = value;
        prevFillAmount = value;
    }
}
