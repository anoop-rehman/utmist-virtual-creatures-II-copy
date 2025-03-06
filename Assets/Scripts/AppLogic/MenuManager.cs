using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using KSS;

//public enum CurrentMenu { MAIN, EVOLUTION_SETTINGS, EVOLUTION_LS, EVOLUTION_SERVER}
public class MenuManager : MonoBehaviour
{
    //[SerializeField]
    //private CurrentMenu currentMenu = CurrentMenu.MAIN;

    [Header("Menus")]
    public GameObject mainMenu;
    public GameObject optionsMenu;
    public GameObject creditsMenu;
    public GameObject chooseEvolution;
    public GameObject editEvolution;
    public GameObject evolutionSettingsMenu;
    public EvolutionViewMenu viewEvolution;
    // TEMP
    [Header("Temporary References")]
    public CreatureGenotypeScriptableObject templateCGSO;

    private List<GameObject> menus;
    private List<EvolutionChooseItem> chooseItems;

    [Header("Prefab References")]
    public EvolutionChooseItem evolutionChooseItemPrefab;

    // settings ui
    [Header("UI Components")]
    public GameObject saveTitle;
    public GameObject saveTitleInput;
    public GameObject evolutionSelectionContent;
    public InputField populationInput;
    public InputField generationInput;
    public InputField envCountInput;
    public InputField maxSegmentsInput;
    public InputField ratioNumeratorInput;
    public InputField ratioDenominatorInput;
    public Toggle lockNeuralMutationsToggle;
    public Toggle lockPhysicalMutationsToggle;

    private string saveName = "New Evolution";

    // evolution settings values
    private CreatureGenotype initialGenotype;
    private int populationSize;
    private int totalGenerations;
    private int envCount;
    private int maxSegments;
    private float ratioNumerator;
    private float ratioDenominator;
    private bool lockNeuralMutations;
    private bool lockPhysicalMutations;

    public static MenuManager instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject); // Can't have two managers active at once
        }
        else
        {
            instance = this;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        menus = new List<GameObject>() { mainMenu, optionsMenu, creditsMenu, chooseEvolution, editEvolution, evolutionSettingsMenu, viewEvolution.gameObject };
        ShowMainMenu();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowZoo()
    {
        SceneManager.LoadScene("Zoo Menu");
    }

    public void ShowOptions()
    {
        menus.ForEach(o => o.SetActive(false));
        optionsMenu.SetActive(true);
    }

    public void ShowCredits()
    {
        menus.ForEach(o => o.SetActive(false));
        creditsMenu.SetActive(true);
    }

    public void ShowSandbox()
    {
        Debug.Log("Show sandbox scene");
        SceneManager.LoadScene("OceanEnv");
    }

    public void ShowMainMenu()
    {
        menus.ForEach(o => o.SetActive(false));
        mainMenu.SetActive(true);
    }

    public void ShowChooseEvolutionMenu()
    {
        menus.ForEach(o => o.SetActive(false));

        string[] fileArray = Directory.GetFiles(OptionsPersist.instance.VCSaves, "*.save");
        chooseItems = new List<EvolutionChooseItem>();

        float spacing = 10f;  // spacing between items
        float itemHeight = 100f;  // height of each item, adjust as necessary
        float totalHeight = fileArray.Length * (itemHeight + spacing);  // total height needed for all items

        RectTransform rt = evolutionSelectionContent.transform.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, totalHeight);

        float currentYPosition = -spacing;  // start from the top with a little margin

        foreach (Transform child in evolutionSelectionContent.transform)
        {
            Destroy(child.gameObject);
        }

        //evolutionSelectionContent.transform.localScale = scale;
        foreach (string filePath in fileArray)
        {
            EvolutionChooseItem evi = Instantiate(evolutionChooseItemPrefab, evolutionSelectionContent.transform);
            evi.transform.SetParent(evolutionSelectionContent.transform, false);
            evi.transform.localScale = Vector3.one;
            // evi.transform.localPosition = new Vector3(0, currentYPosition, 0f);

            // Set the anchor to top-middle
            RectTransform eviRt = evi.GetComponent<RectTransform>();
            eviRt.anchorMin = new Vector2(0.5f, 1);
            eviRt.anchorMax = new Vector2(0.5f, 1);
            eviRt.pivot = new Vector2(0.5f, 1);

            eviRt.sizeDelta = new Vector2(eviRt.sizeDelta.x, itemHeight);  // ensure the item has the correct height
            eviRt.anchoredPosition = new Vector2(0, currentYPosition);  // set anchored position

            evi.Setup(filePath);
            chooseItems.Add(evi);

            currentYPosition -= (itemHeight + spacing);  // move the position downward for the next item
            Debug.Log(filePath);
        }

        chooseEvolution.SetActive(true);
    }

    public void ShowViewEvolutionMenu(string filePath){
        menus.ForEach(o => o.SetActive(false));
        viewEvolution.gameObject.SetActive(true);
        KSSSave currentSave = (KSSSave)KSSSave.LoadData(filePath, true);
        viewEvolution.SetEvolution(currentSave);
    }

    public void ShowEvolutionSettingsMenu()
    {
        menus.ForEach(o => o.SetActive(false));

        populationSize = int.Parse(populationInput.text);
        totalGenerations = int.Parse(generationInput.text);
        envCount = int.Parse(envCountInput.text);
        maxSegments = int.Parse(maxSegmentsInput.text);

        ratioNumerator = float.Parse(ratioNumeratorInput.text);

        ratioDenominator = float.Parse(ratioDenominatorInput.text);

        lockNeuralMutations = lockNeuralMutationsToggle.isOn;

        lockPhysicalMutations = lockPhysicalMutationsToggle.isOn;

        evolutionSettingsMenu.SetActive(true);
}


    public void EditSaveName()
    {
        saveTitle.SetActive(false);
        saveTitleInput.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void SetSaveName(string input)
    {
        saveName = input;
        saveTitle.SetActive(true);
        saveTitle.GetComponent<Text>().text = input;
        saveTitleInput.SetActive(false);
    }

    public void SetPopulationSize(string input)
    {
        populationSize = int.Parse(input);
    }

    public void SetTotalGenerations(string input)
    {
        totalGenerations = int.Parse(input);
    }

    public void SetEnvCount(string input)
    {
        envCount = int.Parse(input);
    }

    public void SetMaxSegments(string input)
    {
        maxSegments = int.Parse(input);
    }

    public void SetSurvivalRatioNumerator(string input)
    {
        ratioNumerator = float.Parse(input);
    }

    public void SetSurvivalRatioDenominator(string input)
    {
        ratioDenominator = float.Parse(input);
    }

    public void LockNeuralMutation(bool input)
    {
        lockNeuralMutations = input;
    }

    public void LockPhysicalMutation(bool input)
    {
        lockPhysicalMutations = input;
    }


    public void LoadLocal()
    {
        // Compile data from settings window into TrainingSave
        KSSSettings optimizationSettings = new KSSSettings(populationSize, totalGenerations, ratioNumerator / ratioDenominator);
        optimizationSettings.numEnvs = envCount;
        optimizationSettings.mp = new MutateGenotype.MutationPreferenceSetting();
        optimizationSettings.mp.mutateNeural = !lockNeuralMutations;
        optimizationSettings.mp.mutateMorphology = !lockPhysicalMutations;
        optimizationSettings.mp.maxSegments = maxSegments;
        // This line takes from a template creature genotype found in a Unity PropertyDrawer
        //optimizationSettings.initialGenotype = templateCGSO == null ? null : templateCGSO.cg;
        //optimizationSettings.initialGenotype = CreatureGenotype.LoadData("/Leaper.creature", false); // null means start w/ random creatures. TODO: Non-null will mean spawn that with mutations!
        //optimizationSettings.initialGenotype = CreatureGenotype.LoadData("/Users/anooprehman/Virtual Creatures/Creatures/carolgen5walker.creature", true); // null means start w/ random creatures. TODO: Non-null will mean spawn that with mutations!
        optimizationSettings.initialGenotype = null;



        //TrainingSettings ts = new TrainingSettings(optimizationSettings, new OceanEnvSettings());
        TrainingSettings ts = new TrainingSettings(optimizationSettings, new FloorEnvSettings());


        KSSSave save = new KSSSave();
        save.isNew = true;
        save.ts = ts;
        save.saveName = saveName;

        // Send to EvolutionSettingsPersist
        EvolutionSettingsPersist esp = EvolutionSettingsPersist.instance;
        if (esp == null)
        {
            throw new Exception("No EvolutionSettingsPersist instance found.");
        }
        esp.save = save;

        bool valid = save.IsValid();

        if (valid)
        {
            // Load env runner
            SceneManager.LoadScene("LocalEnvRunner");
        } else {
            Debug.Log("Invalid!");
            // TODO: Show on UI that it's invalid
        }
    }
}
