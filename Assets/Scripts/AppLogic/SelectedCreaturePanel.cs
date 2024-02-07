using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using System;

public class SelectedCreaturePanel : MonoBehaviour
{
    public Text text;
    public static SelectedCreaturePanel instance;
    private CreatureSpawner creatureSpawner;
    private Creature currentCreature;
    private Creature currentCreatureClone;
    private string currentRewardString;
    private string currentCreatureName;
    private bool savedCurrent = false;
    public Transform selectedCreatureContainer;
    public Button saveButton;
    private int cloneLayer;

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

    private void Start()
    {
        creatureSpawner = CreatureSpawner.instance;
        cloneLayer = LayerMask.NameToLayer("CreatureClone");
    }

    private void Update()
    {
        if (currentCreatureClone == null)
        {
            text.text = "No Selected Creature.";
            return;
        }

        if (currentCreature != null){
            currentRewardString = currentCreature.totalReward.ToString("F3");
            currentCreatureName = currentCreature.cg.name;
        }

        text.text = string.Format("{0}, Total Reward: {1}", currentCreatureName, currentRewardString);
    }

    public void SaveCreature()
    {
        if (currentCreatureClone == null) return;
        savedCurrent = true;
        saveButton.interactable = false;
        saveButton.GetComponentInChildren<Text>().text = "Saved!";

        string path = Path.Combine(OptionsPersist.VCCreatures, currentCreatureClone.cg.name + ".creature");

        //DateTime now = DateTime.Now;

        //// Format the DateTime as a string in the desired format "09:59AM_07FEB2024"
        //string formattedTime = now.ToString("hhmmtt_MMddyyyy").ToUpper().Replace(":", "/");

        //// Insert an underscore between the time and date parts
        //formattedTime = formattedTime.Insert(6, "_");

        //// Define the folder name
        //string folderName = formattedTime + "_EVOLUTIONRUN";

        //// Combine the base path with the new folder name
        //string basePath = Path.Combine(OptionsPersist.VCCreatures, folderName);

        //// Ensure the directory exists
        //if (!Directory.Exists(basePath))
        //{
        //    Directory.CreateDirectory(basePath);
        //}

        //// Combine the new base path with the creature file name to get the final path
        //path = Path.Combine(basePath, currentCreatureClone.cg.name + ".creature");

        currentCreatureClone.cg.SaveData(path, true, false);
        Debug.Log(string.Format("Saved {0} to {1}", currentCreatureClone.cg.name, path));
    }

    public void SaveCreature(string path)
    {
        if (currentCreatureClone == null) return;
        savedCurrent = true;
        saveButton.interactable = false;
        saveButton.GetComponentInChildren<Text>().text = "Saved!";

        currentCreatureClone.cg.SaveData(path, true, false);
        Debug.Log(string.Format("Saved {0} to {1}", currentCreatureClone.cg.name, path));
    }

    public void UpdateSelectedCreature(Creature creatureToClone){
        currentCreature = creatureToClone;
        CreatureGenotype creatureGenotypeToClone = creatureToClone.cg;
        if (currentCreatureClone == null || currentCreatureClone.cg != creatureGenotypeToClone) {
            // Destroy previously cloned creature
            savedCurrent = false;
            saveButton.interactable = true;
            saveButton.GetComponentInChildren<Text>().text = "Save";

            if (currentCreatureClone != null)
            {
                Destroy(currentCreatureClone.gameObject);
            }

            // Instantiate new creature
            Creature creatureClone = creatureSpawner.SpawnCreature(creatureGenotypeToClone, Vector3.zero);
            creatureClone.transform.parent = selectedCreatureContainer;

            // Make segments non-colliding and non-rendering
            foreach (Segment s in creatureClone.segments)
            {
                s.gameObject.GetComponent<Rigidbody>().detectCollisions = false;
                s.gameObject.layer = cloneLayer;
                s.transform.Find("Graphic").gameObject.layer = cloneLayer;
            }

            // Disable creature neurons and motion
            creatureClone.SetAlive(false);
            currentCreatureClone = creatureClone;

            // Target camera to creature
            CreatureViewerController.instance.SetCreature(currentCreatureClone);
        }
    }
}
