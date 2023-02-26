using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Transform buildingMenu;
    public GameObject buildingButtonPrefab;
    public Transform resourcesUIParent;
    public GameObject gameResourceDisplayPrefab;
    public GameObject infoPanel;
    public GameObject gameResourceCostPrefab;
    public Color invalidTextColor;
    public Transform selectedUnitsListParent;
    public GameObject selectedUnitDisplayPrefab;
    public Transform selectionGroupsParent;
    public GameObject selectedUnitMenu;
    public GameObject unitSkillButtonPrefab;
    public GameObject gameSettingsPanel;
    public Transform gameSettingsMenusParent;
    public TextMeshProUGUI gameSettingsContentName;
    public Transform gameSettingsContentParent;
    public GameObject gameSettingsMenuButtonPrefab;
    public GameObject gameSettingsParameterPrefab;
    public GameObject sliderPrefab;
    public GameObject togglePrefab;
    public GameObject overlayTint;
    
    [Header("Placed Building Production")]
    public RectTransform placedBuildingProductionRectTransform;

    private Unit _selectedUnit;
    private RectTransform _selectedUnitContentRectTransform;
    private TextMeshProUGUI _selectedUnitTitleText;
    private TextMeshProUGUI _selectedUnitLevelText;
    private Transform _selectedUnitResourcesProductionParent;
    private Transform _selectedUnitActionButtonsParent;
    private BuildingPlacer _buildingPlacer;
    private Dictionary<InGameResource, TextMeshProUGUI> _resourceTexts;
    private Dictionary<string, Button> _buildingButtons;
    private TextMeshProUGUI _infoPanelTitleText;
    private TextMeshProUGUI _infoPanelDescriptionText;
    private Transform _infoPanelResourcesCostParent;
    private Dictionary<string, GameParameters> _gameParameters;

    private void OnEnable()
    {
        EventManager.AddListener("UpdateResourceTexts",_OnUpdateResourceTexts);
        EventManager.AddListener("CheckBuildingButtons",CheckBuildingButtons);
        EventManager.AddListener((string)"HoverBuildingButton",(UnityAction<object>)OnHoverBuildingButton);
        EventManager.AddListener("UnhoverBuildingButton",OnUnHoverBuildingButton);
        EventManager.AddListener((string)"SelectUnit", (UnityAction<object>)_OnSelectUnit);
        EventManager.AddListener((string)"DeselectUnit", (UnityAction<object>)_OnDeselectUnit);
        EventManager.AddListener("UpdatePlacedBuildingProduction", _OnUpdatePlacedBuildingProduction);
        EventManager.AddListener("PlaceBuildingOn", _OnPlaceBuildingOn);
        EventManager.AddListener("PlaceBuildingOff", _OnPlaceBuildingOff);
    }
    
    private void OnDisable()
    {
        EventManager.RemoveListener("UpdateResourceTexts",_OnUpdateResourceTexts);
        EventManager.RemoveListener("CheckBuildingButtons",CheckBuildingButtons);
        EventManager.RemoveListener((string)"HoverBuildingButton",(UnityAction<object>)OnHoverBuildingButton);
        EventManager.RemoveListener("UnhoverBuildingButton",OnUnHoverBuildingButton);
        EventManager.RemoveListener("UpdatePlacedBuildingProduction", _OnUpdatePlacedBuildingProduction);
        EventManager.RemoveListener("PlaceBuildingOn", _OnPlaceBuildingOn);
        EventManager.RemoveListener("PlaceBuildingOff", _OnPlaceBuildingOff);
    }

   

    private void Awake()
    {
        _buildingPlacer = GetComponent<BuildingPlacer>();
        _resourceTexts = new Dictionary<InGameResource, TextMeshProUGUI>();
        foreach (var pair in Globals.GAME_RESOURCES)
        {
            GameObject g = Instantiate(gameResourceDisplayPrefab, resourcesUIParent);
            g.transform.Find("Icon").GetComponent<Image>().sprite = 
                Resources.Load<Sprite>($"Textures/GameResources/{pair.Key}");
            g.name = pair.Key.ToString();
            _resourceTexts.Add(pair.Key,g.transform.Find("Text").GetComponent<TextMeshProUGUI>());
            _SetResourceText(pair.Key,pair.Value.Amount);
            placedBuildingProductionRectTransform.gameObject.SetActive(false);
        }

        // create buttons for each building type
        _buildingButtons = new Dictionary<string, Button>();
        for (int i = 0; i < Globals.BUILDING_DATA.Length; i++)
        {
            GameObject button = GameObject.Instantiate(
                buildingButtonPrefab,
                buildingMenu);
            string code = Globals.BUILDING_DATA[i].code;
            button.name = code;
            button.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = Globals.BUILDING_DATA[i].unitName;
            Button b = button.GetComponent<Button>();
            _AddBuildingButtonListener(b, i);
            
            _buildingButtons[code] = b;
            if (!Globals.BUILDING_DATA[i].CanBuy())
            {
                b.interactable = false;
            }
            button.GetComponent<BuildingButton>().Initialize(Globals.BUILDING_DATA[i]);
        }

        _infoPanelTitleText = infoPanel.transform.Find("content/title").GetComponent<TextMeshProUGUI>();
        _infoPanelDescriptionText = infoPanel.transform.Find("content/description").GetComponent<TextMeshProUGUI>();
        _infoPanelResourcesCostParent = infoPanel.transform.Find("content/cost");
        ShowInfoPanel(false);

        for (int i = 1; i < 10; i++)
        {
            ToggleSelectionGroupButton(i, false);
        }
        
        Transform selectedUnitMenuTransform = selectedUnitMenu.transform;
        _selectedUnitContentRectTransform = selectedUnitMenuTransform
            .Find("Content").GetComponent<RectTransform>();
        _selectedUnitTitleText = selectedUnitMenuTransform
            .Find("Content/Title").GetComponent<TextMeshProUGUI>();
        _selectedUnitLevelText = selectedUnitMenuTransform
            .Find("Content/Level").GetComponent<TextMeshProUGUI>();
        _selectedUnitResourcesProductionParent = selectedUnitMenuTransform
            .Find("Content/ResourcesProduction");
        _selectedUnitActionButtonsParent = selectedUnitMenuTransform
            .Find("SpecificActions");
        
        _ShowSelectedUnitMenu(false);
        gameSettingsPanel.SetActive(false);
        
        GameParameters[] gameParametersList = Resources.LoadAll<GameParameters>(
            "ScriptableObjects/Parameters");
        _gameParameters = new Dictionary<string, GameParameters>();
        foreach (GameParameters p in gameParametersList)
            _gameParameters[p.GetParametersName()] = p;
        _SetupGameSettingsPanel();
    }
    
    private void _OnPlaceBuildingOn()
    {
        placedBuildingProductionRectTransform.gameObject.SetActive(true);
    }

    private void _OnPlaceBuildingOff()
    {
        placedBuildingProductionRectTransform.gameObject.SetActive(false);
    }

    private void _OnUpdatePlacedBuildingProduction(object data)
    {
        object[] values = (object[])data;
        Dictionary<InGameResource, int> production = (Dictionary<InGameResource, int>) values[0];
        Vector3 pos = (Vector3) values[1];
        foreach (Transform child in placedBuildingProductionRectTransform.gameObject.transform)
            Destroy(child.gameObject);
        GameObject g;
        Transform t;
        foreach (KeyValuePair<InGameResource, int> pair in production)
        {
            g = GameObject.Instantiate(
                gameResourceCostPrefab,
                placedBuildingProductionRectTransform.transform);
            t = g.transform;
            t.Find("Text").GetComponent<TextMeshProUGUI>().text = $"+{pair.Value}";
            t.Find("Icon").GetComponent<Image>().sprite = Resources.Load<Sprite>($"Textures/GameResources/{pair.Key}");
            placedBuildingProductionRectTransform.sizeDelta = new Vector2(80, 24 * production.Count);
            placedBuildingProductionRectTransform.anchoredPosition =
                (Vector2) Camera.main.WorldToScreenPoint(pos)
                + Vector2.right * 40f
                + Vector2.up * 10f;
        }
    }

    private void _SetupGameSettingsPanel()
    {
        List<String> availableMenus = new List<string>();
        foreach (var parameter in _gameParameters.Values)
        {
            if (parameter.FieldsToShowInGame.Count == 0)
                continue;

            var spawnedMenu =Instantiate(gameSettingsMenuButtonPrefab, gameSettingsMenusParent);
            var label =spawnedMenu.GetComponentInChildren<TextMeshProUGUI>();
            var parameterName = parameter.GetParametersName();
            var button = spawnedMenu.GetComponent<Button>();
            if (label)
                label.text = parameterName;
            availableMenus.Add(parameterName);
            _AddGameSettingsPanelMenuListener(button, parameterName);
        }
    }
    
    private void _AddGameSettingsPanelMenuListener(Button b, string menu)
    {
        b.onClick.AddListener(() => _SetGameSettingsContent(menu));
    }

    private void _SetGameSettingsContent(string menuName)
    {
        gameSettingsContentName.text = menuName;

        //destroy all previous menus
        foreach (Transform child in gameSettingsContentParent)
        {
            Destroy(child.gameObject);
        }

        var parameters = _gameParameters[menuName];
        var parameterType = parameters.GetType();
        int index = 0;
        GameObject gWrapper, gEditor;
        RectTransform rtWrapper, rtEditor;
        int i = 0;
        float contentWidth = 400f;
        float parameterNameWidth = 200f;
        float fieldHeight = 32f;
        foreach (var fieldName in parameters.FieldsToShowInGame)
        {
            gWrapper = Instantiate(gameSettingsParameterPrefab, gameSettingsContentParent);
            var gameSettingsLabel = gWrapper.GetComponent<TextMeshProUGUI>();
            gameSettingsLabel.text = Utils.CapitalizeText(fieldName);
            var field = parameterType.GetField(fieldName);
            var fieldType = field.FieldType;
            
            gEditor = null;
            if (fieldType == typeof(bool))
            {
                gEditor = Instantiate(togglePrefab);
                var toggle = gEditor.GetComponent<Toggle>();
                toggle.isOn = (bool) field.GetValue(parameters);
                toggle.onValueChanged.AddListener(delegate
                {
                    _OnGameSettingsToggleValueChanged(parameters, field, fieldName, toggle);
                });
            }
            else if (fieldType == typeof(int) || fieldType == typeof(float))
            {
                bool isRange = Attribute.IsDefined(field, typeof(RangeAttribute), false);
                if (isRange)
                {
                    var rangeAttribute = (RangeAttribute)Attribute.GetCustomAttribute(field, typeof(RangeAttribute), false);
                    if (rangeAttribute != null)
                    {
                        gEditor = Instantiate(sliderPrefab);
                        var slider = gEditor.GetComponent<Slider>();
                        slider.maxValue = rangeAttribute.max;
                        slider.minValue = rangeAttribute.min;
                        slider.value = fieldType == typeof(int)
                            ? (int)field.GetValue(parameters)
                            : (float)field.GetValue(parameters);
                        slider.onValueChanged.AddListener(delegate
                        {
                            _OnGameSettingsSliderValueChanged(parameters, field, fieldName, slider);
                        });
                    }
                }
            }
            
            rtWrapper = gWrapper.GetComponent<RectTransform>();
            rtWrapper.anchoredPosition = new Vector2(0f, -i * fieldHeight);
            rtWrapper.sizeDelta = new Vector2(contentWidth, fieldHeight);

            if (gEditor != null)
            {
                gEditor.transform.SetParent(gWrapper.transform);
                rtEditor = gEditor.GetComponent<RectTransform>();
                rtEditor.anchoredPosition = new Vector2((parameterNameWidth + 16f), 0f);
                rtEditor.sizeDelta = new Vector2(rtWrapper.sizeDelta.x - (parameterNameWidth + 16f), fieldHeight);
            }

            i++;
        }
        
        RectTransform rt = gameSettingsContentParent.GetComponent<RectTransform>();
        Vector2 size = rt.sizeDelta;
        size.y = i * fieldHeight;
        rt.sizeDelta = size;
    }

    private void _OnGameSettingsToggleValueChanged(GameParameters parameters, FieldInfo field, string gameParameter, Toggle change)
    {
        field.SetValue(parameters,change.isOn);
        EventManager.TriggerEvent($"UpdateGameParameter:{gameParameter}", change.isOn);
    }
    
    private void _OnGameSettingsSliderValueChanged(
        GameParameters parameters,
        FieldInfo field,
        string gameParameter,
        Slider change
    )
    {
        if (field.FieldType == typeof(int))
            field.SetValue(parameters, (int) change.value);
        else
            field.SetValue(parameters, change.value);
        EventManager.TriggerEvent($"UpdateGameParameter:{gameParameter}", change.value);
    }
    public void ToggleSelectionGroupButton(int groupIndex, bool on)
    {
        selectionGroupsParent.Find(groupIndex.ToString()).gameObject.SetActive(on);
    }
    
    public void ToggleGameSettingsPanel()
    {
        bool showGameSettingsPanel = !gameSettingsPanel.activeSelf;
        gameSettingsPanel.SetActive(showGameSettingsPanel);
        EventManager.TriggerEvent(showGameSettingsPanel ? "PauseGame" : "ResumeGame");
        overlayTint.gameObject.SetActive(showGameSettingsPanel);
    }
    
    private void _SetResourceText(InGameResource resource, int value)
    {
        _resourceTexts[resource].text = value.ToString();
    }
    private void _AddBuildingButtonListener(Button b, int i)
    {
        b.onClick.AddListener(() => _buildingPlacer.SelectPlacedBuilding(i));
    }

    private void OnHoverBuildingButton(object data)
    {
        
        SetInfoPanel((UnitData)data);
        ShowInfoPanel(true);
    }
    private void OnUnHoverBuildingButton()
    {
        ShowInfoPanel(false);
    }
    
    private void _OnSelectUnit(object data)
    {
        _AddSelectedUnitToUIList((Unit)data);
        _SetSelectedUnitMenu((Unit)data);
        _ShowSelectedUnitMenu(true);
    }

    private void _OnDeselectUnit(object data)
    {
        var unitData = data as Unit;
        _RemoveSelectedUnitFromUIList(unitData.Code);
        if (Globals.SELECTED_UNITS.Count == 0)
            _ShowSelectedUnitMenu(false);
        else
            _SetSelectedUnitMenu(Globals.SELECTED_UNITS[Globals.SELECTED_UNITS.Count - 1].Unit);
    }

    public void CheckBuildingButtons()
    {
        foreach (UnitData data in Globals.BUILDING_DATA)
        {
            _buildingButtons[data.code].interactable = data.CanBuy();
        }
    }
    
    public void _AddSelectedUnitToUIList(Unit unit)
    {
        // if there is another unit of the same type already selected,
        // increase the counter
        Transform alreadyInstantiatedChild = selectedUnitsListParent.Find(unit.Code);
        if (alreadyInstantiatedChild != null)
        {
            TextMeshProUGUI t = alreadyInstantiatedChild.Find("Count").GetComponent<TextMeshProUGUI>();
            int count = int.Parse(t.text);
            t.text = (count + 1).ToString();
        }
        // else create a brand new counter initialized with a count of 1
        else
        {
            GameObject g = GameObject.Instantiate(
                selectedUnitDisplayPrefab, selectedUnitsListParent);
            g.name = unit.Code;
            Transform t = g.transform;
            t.Find("Count").GetComponent<TextMeshProUGUI>().text = "1";
            t.Find("Name").GetComponent<TextMeshProUGUI>().text = unit.Data.unitName;
        }
    }
    
    public void _RemoveSelectedUnitFromUIList(string code)
    {
        Transform listItem = selectedUnitsListParent.Find(code);
        if (listItem == null) return;
        TextMeshProUGUI t = listItem.Find("Count").GetComponent<TextMeshProUGUI>();
        int count = int.Parse(t.text);
        count -= 1;
        if (count == 0)
            DestroyImmediate(listItem.gameObject);
        else
            t.text = count.ToString();
    }
    
    
    public void _OnUpdateResourceTexts()
    {
        foreach (KeyValuePair<InGameResource, GameResource> pair in Globals.GAME_RESOURCES)
        {
            _SetResourceText(pair.Key, pair.Value.Amount);
        }
    }
    
    public void ShowInfoPanel(bool show)
    {
        infoPanel.SetActive(show);
    }
   
    public void SetInfoPanel(UnitData data)
    {
        // update texts
        if (data.code != "")
            _infoPanelTitleText.text = data.unitName;
        if (data.description != "")
            _infoPanelDescriptionText.text = data.description;

        // clear resource costs and reinstantiate new ones
        foreach (Transform child in _infoPanelResourcesCostParent)
            Destroy(child.gameObject);

        if (data.cost.Count > 0)
        {
            GameObject g;
            Transform t;
            foreach (ResourceValue resource in data.cost)
            {
                g = GameObject.Instantiate(gameResourceCostPrefab, _infoPanelResourcesCostParent);
                t = g.transform;
                var title = t.Find("Text").GetComponent<TextMeshProUGUI>();
                title.text = resource.amount.ToString();
                t.Find("Icon").GetComponent<Image>().sprite = Resources.Load<Sprite>
                    ($"Textures/GameResources/{resource.code}");

                if (Globals.GAME_RESOURCES[resource.code].Amount<resource.amount)
                {
                    title.color = invalidTextColor;
                }

            }
        }
    }
    
    private void _SetSelectedUnitMenu(Unit unit)
    {
        // update texts
        _selectedUnitTitleText.text = unit.Data.unitName;
        _selectedUnitLevelText.text = $"Level {unit.Level}";
        // clear resource production and reinstantiate new one
        foreach (Transform child in _selectedUnitResourcesProductionParent)
            Destroy(child.gameObject);
        if (unit.Production.Count > 0)
        {
            GameObject g; Transform t;
            foreach (var resource in unit.Production)
            {
                g = Instantiate(
                    gameResourceCostPrefab, _selectedUnitResourcesProductionParent);
                t = g.transform;
                t.Find("Text").GetComponent<TextMeshProUGUI>().text = $"+{resource.Value}";
                t.Find("Icon").GetComponent<Image>().sprite = Resources.Load<Sprite>($"Textures/GameResources/{resource.Key}");
            }
        }
        
        _selectedUnit = unit;
        // ...
        // clear skills and reinstantiate new ones
        foreach (Transform child in _selectedUnitActionButtonsParent)
            Destroy(child.gameObject);
        if (unit.SkillManagers.Count > 0)
        {
            GameObject g; Transform t; Button b;
            for (int i = 0; i < unit.SkillManagers.Count; i++)
            {
                g = GameObject.Instantiate(
                    unitSkillButtonPrefab, _selectedUnitActionButtonsParent);
                t = g.transform;
                b = g.GetComponent<Button>();
                unit.SkillManagers[i].SetButton(b);
                t.Find("Text").GetComponent<TextMeshProUGUI>().text =
                    unit.SkillManagers[i].skill.skillName;
                _AddUnitSkillButtonListener(b, i);
            }
        }
    }
    
    private void _ShowSelectedUnitMenu(bool show)
    {
        selectedUnitMenu.SetActive(show);
    }
    private void _AddUnitSkillButtonListener(Button b, int i)
    {
        b.onClick.AddListener(() => _selectedUnit.TriggerSkill(i));
    }
}
