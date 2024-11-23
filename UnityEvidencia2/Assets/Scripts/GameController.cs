// GameController.cs
using UnityEngine;
using System.Collections;
using TMPro; // Make sure to include TextMeshPro if you're using it

public class GameController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject alertTextObject;
    [SerializeField] private float alertDisplayTime = 3f;
    
    [Header("Alarm Settings")]
    [SerializeField] private Light[] alarmLights;
    [SerializeField] private Color alarmColor = Color.red;
    [SerializeField] private float alarmBlinkRate = 1f;
    
    private Color[] originalLightColors;
    private bool simulationEnded = false;
    private Coroutine alarmCoroutine;

    void Start()
    {
        // Store original light colors
        originalLightColors = new Color[alarmLights.Length];
        for (int i = 0; i < alarmLights.Length; i++)
        {
            originalLightColors[i] = alarmLights[i].color;
        }
        
        if (alertTextObject != null)
        {
            alertTextObject.SetActive(false);
        }
    }

    public void TriggerGeneralAlarm(bool isRealThreat)
    {
        if (simulationEnded) return;

        if (isRealThreat)
        {
            // Stop any existing alarm coroutine
            if (alarmCoroutine != null)
            {
                StopCoroutine(alarmCoroutine);
            }
            alarmCoroutine = StartCoroutine(BlinkAlarmLights());
        }
        else
        {
            ShowFalseAlarm();
        }
    }

    private IEnumerator BlinkAlarmLights()
    {
        bool isOn = true;
        while (!simulationEnded)
        {
            foreach (Light light in alarmLights)
            {
                light.color = isOn ? alarmColor : Color.black;
            }
            isOn = !isOn;
            yield return new WaitForSeconds(alarmBlinkRate);
        }
    }

    private void ShowFalseAlarm()
    {
        if (alertTextObject != null)
        {
            alertTextObject.SetActive(true);
            StartCoroutine(HideAlertText());
        }
    }

    private IEnumerator HideAlertText()
    {
        yield return new WaitForSeconds(alertDisplayTime);
        if (alertTextObject != null)
        {
            alertTextObject.SetActive(false);
        }
    }

    public void EndSimulation()
    {
        if (!simulationEnded)
        {
            simulationEnded = true;
            
            // Stop alarm blinking
            if (alarmCoroutine != null)
            {
                StopCoroutine(alarmCoroutine);
            }
            
            // Restore original light colors
            for (int i = 0; i < alarmLights.Length; i++)
            {
                alarmLights[i].color = originalLightColors[i];
            }
            
            Debug.Log("Simulation ended successfully!");
            
            // You can add additional end-game logic here
            // Such as showing a victory screen, stats, etc.
        }
    }
}