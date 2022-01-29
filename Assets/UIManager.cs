using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

using System.Collections.Generic;


public class UIManager : MonoBehaviour
{

    public GameObject canvas;
    public MantleManager mantleManager;

    List<GameObject> uiElements = new List<GameObject>();

    public GameObject uiPanel;
    public GameObject uiToggle;
    public GameObject uiCellCentreAttractMultSlider;
    public GameObject uiTargetAreaMultSlider;
    public GameObject uiRespawnDropdown;
    public GameObject uiStrengthDropdown;

    Dictionary<string, int> respawnLookup = new Dictionary<string, int>();
    Dictionary<string, int> strengthLookup = new Dictionary<string, int>();

    bool ignoreSliderEvent = false;


    public void Start() {
        if (canvas == null) { canvas = transform.Find("Canvas").gameObject; }
        if (mantleManager == null) { mantleManager = FindObjectOfType<MantleManager>(); }


        /*
        if (uiToggle == null) { uiToggle = canvas.transform.Find("UIToggle").gameObject; }
        if (uiCellCentreAttractMultSlider == null) { uiCellCentreAttractMultSlider = canvas.transform.Find("CellCentreAttractionMult").gameObject; }
        if (uiTargetAreaMultSlider == null) { uiTargetAreaMultSlider = canvas.transform.Find("CellTargetAreaMult").gameObject; }
        if (uiRespawnDropdown == null) { uiRespawnDropdown = canvas.transform.Find("RespawnMethod").gameObject; }
        if (uiStrengthDropdown == null) { uiStrengthDropdown = canvas.transform.Find("StrengthMethod").gameObject; }
        */
        uiPanel = FindUIElement("Panel");
        uiToggle = FindUIElement("UIToggle");
        uiCellCentreAttractMultSlider = FindUIElement("CellCentreAttractionMult");
        uiTargetAreaMultSlider = FindUIElement("CellTargetAreaMult");
        uiRespawnDropdown = FindUIElement("RespawnMethod");
        uiStrengthDropdown = FindUIElement("StrengthMethod");

        SetUIToggleText();


        ignoreSliderEvent = true;
        float value = mantleManager.centroidAttraction;
        float sliderValue = Mathf.Log10(0.5f * (value + Mathf.Sqrt(value * value + 4))); // inverse function as that of the slider
        sliderValue = 0.25f * sliderValue + 0.5f;
        sliderValue = Mathf.Clamp(sliderValue, 0f, 1f);
        uiCellCentreAttractMultSlider.GetComponent<Slider>().value = sliderValue;
        SetSliderTextValue(uiCellCentreAttractMultSlider, value);

        value = mantleManager.targetAreaFactor;
        sliderValue = Mathf.Log10(value); // inverse function as that of the slider
        sliderValue = 0.25f * sliderValue + 0.5f;
        sliderValue = Mathf.Clamp(sliderValue, 0f, 1f);
        uiTargetAreaMultSlider.GetComponent<Slider>().value = sliderValue;
        SetSliderTextValue(uiTargetAreaMultSlider, value);

        ignoreSliderEvent = false;

        PopulateRespawnDropdown();
        PopulateStrengthDropdown();

        foreach (var obj in uiElements) {
            obj.SetActive(false);
        }
        uiToggle.SetActive(true);

    }



    bool uiShown = false;
    public void _ToggleUI() {
        uiShown = !uiShown;
        SetUIToggleText();
    }


    void SetSliderTextValue(GameObject slider, float value) {
        slider.transform.Find("InputField").GetComponent<InputField>().text = value.ToString("0.000");
    }

    public void _OnChangeCellAttractionSlider() {
        if (!ignoreSliderEvent) {
            float newValue = uiCellCentreAttractMultSlider.GetComponent<Slider>().value; // float between 0 and 1
            newValue = newValue * 4 - 2;        // from -2 to +2
            newValue = Mathf.Pow(10, newValue) - Mathf.Pow(10, -newValue); // from -100 to +100 exponential.

            SetSliderTextValue(uiCellCentreAttractMultSlider, newValue);

            mantleManager.centroidAttraction = newValue;
        }
    }

    public void _OnChangeCellAttractionField() {
        float newValue = float.Parse(uiCellCentreAttractMultSlider.transform.Find("InputField").GetComponent<InputField>().text);

        float sliderValue = Mathf.Log10(0.5f * (newValue + Mathf.Sqrt(newValue * newValue + 4) ) ); // inverse function as that of the slider
        sliderValue = 0.25f * sliderValue + 0.5f;
        sliderValue = Mathf.Clamp(sliderValue, 0f, 1f);

        ignoreSliderEvent = true;
        uiCellCentreAttractMultSlider.GetComponent<Slider>().value = sliderValue;
        ignoreSliderEvent = false;

        mantleManager.centroidAttraction = newValue;
    }

    public void _OnChangeTargetAreaSlider() {
        if (!ignoreSliderEvent) {
            float newValue = uiTargetAreaMultSlider.GetComponent<Slider>().value; // float between 0 and 1
            newValue = newValue * 4 - 2f;        // from -2 to +2
            newValue = Mathf.Pow(10, newValue); // from +0.01 to +100 exponential.

            SetSliderTextValue(uiTargetAreaMultSlider, newValue);

            mantleManager.targetAreaFactor = newValue;
        }
    }

    public void _OnChangeTargetAreaField() {
        float newValue = float.Parse(uiTargetAreaMultSlider.transform.Find("InputField").GetComponent<InputField>().text);
        newValue = Mathf.Clamp(newValue, 0.00001f, float.PositiveInfinity);
        float sliderValue = Mathf.Log10(newValue); // inverse function as that of the slider
        sliderValue = 0.25f * sliderValue + 0.5f;
        sliderValue = Mathf.Clamp(sliderValue, 0f, 1f);

        ignoreSliderEvent = true;
        uiTargetAreaMultSlider.GetComponent<Slider>().value = sliderValue;
        ignoreSliderEvent = false;

        mantleManager.targetAreaFactor = newValue;
    }

    public void _OnChangeRespawnMethod() {
        mantleManager.respawnMethod = uiRespawnDropdown.GetComponent<Dropdown>().value;
        //Debug.Log("Respawn Method Changed:" + uiRespawnDropdown.GetComponent<Dropdown>().value);
    }

    public void _OnChangeStrengthMethod() {
        mantleManager.strengthUpdateMethod = uiStrengthDropdown.GetComponent<Dropdown>().value;
    }

    Color hideDefaultColor = new Color(.96f, .96f, .96f, .2f);
    Color showDefaultColor = new Color(.96f, .96f, .96f, 1f);
    
    GameObject FindUIElement(string name) {
        GameObject element = canvas.transform.Find(name).gameObject;
        uiElements.Add(element);
        return element;
    }

    void SetUIToggleText() {
        ColorBlock c = uiToggle.GetComponent<Button>().colors;
        if (uiShown) {
            Debug.Log("Showing UI!");
            uiToggle.transform.Find("Text").GetComponent<Text>().text = "▲ ▲ ▲ ▲";
            c.normalColor = showDefaultColor;
            foreach (var obj in uiElements) {
                obj.SetActive(true);
            }
        }
        else {
            Debug.Log("Hiding UI!");
            uiToggle.transform.Find("Text").GetComponent<Text>().text = "▼ ▼ ▼ ▼";
            c.normalColor = hideDefaultColor;
            foreach (var obj in uiElements) {
                obj.SetActive(false);
            }
            uiToggle.SetActive(true);
        }
        canvas.transform.Find("UIToggle").GetComponent<Button>().colors = c;
    }



    void PopulateRespawnDropdown() {
        uiRespawnDropdown.GetComponent<Dropdown>().options.Clear();

        var methods = mantleManager.respawnMethods;

        var dropdown = uiRespawnDropdown.GetComponent<Dropdown>();
        dropdown.ClearOptions();

        List<Dropdown.OptionData> newOptions = new List<Dropdown.OptionData>();
        for (int i = 0; i < methods.Count; i++) {
            var newOption = new Dropdown.OptionData();
            strengthLookup.Add(methods[i], i);
            newOption.text = methods[i];
            newOptions.Add(newOption);
        }
        dropdown.AddOptions(newOptions);
    }

    void PopulateStrengthDropdown() {
        uiStrengthDropdown.GetComponent<Dropdown>().options.Clear();

        var methods = mantleManager.strengthUpdateMethods;

        var dropdown = uiStrengthDropdown.GetComponent<Dropdown>();
        dropdown.ClearOptions();

        List<Dropdown.OptionData> newOptions = new List<Dropdown.OptionData>();
        for (int i = 0; i < methods.Count; i++) {
            var newOption = new Dropdown.OptionData();
            strengthLookup.Add(methods[i], i);
            newOption.text = methods[i];
            newOptions.Add(newOption);
        }
        dropdown.AddOptions(newOptions);
    }






}
