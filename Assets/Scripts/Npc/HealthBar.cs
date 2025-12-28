using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private bool hideIfValueFull = true;
    [SerializeField] private GameObject healthbarFill;
    [SerializeField] private GameObject backPanel;

    private float current_value = 1f;

    const float healthbarScaleDamp = 0.3f;

    private void Start()
    {
        if(hideIfValueFull)
        {
            healthbarFill.SetActive(false);
            backPanel.SetActive(false);
        }
    }

    public void SetValue(float value)
    {
        value = Mathf.Clamp01(value);
        current_value = value;
        
        if(hideIfValueFull)
        {
            if (current_value == 1f)
            {
                healthbarFill.SetActive(false);
                backPanel.SetActive(false);
            }
            else
            {
                healthbarFill.SetActive(true);
                backPanel.SetActive(true);
            }
        }
    }

    private void FixedUpdate()
    {
        if (healthbarFill.activeInHierarchy)
        {
            if (Mathf.Abs(current_value - healthbarFill.transform.localScale.x) > 0.01f)
                healthbarFill.transform.localScale = new Vector3(Mathf.Lerp(healthbarFill.transform.localScale.x, current_value, healthbarScaleDamp), 1f);
        }
    }
}
