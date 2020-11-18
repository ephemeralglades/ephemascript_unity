using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Ephemascript;
using System;

public class ExampleDialogueHandler : MonoBehaviour
{
    public GameObject dialogueWindow;
    public Text MainDialogue;
    public Text Speaker;
    public List<GameObject> choices;

    private void OnEnable()
    {
        NPC.OnDialogueText += OnDialogueEvent;
        NPC.OnDialogueHide += OnDialogueHideEvent;
        dialogueWindow.SetActive(false);
    }
    private void OnDisable()
    {
        NPC.OnDialogueText -= OnDialogueEvent;
        NPC.OnDialogueHide -= OnDialogueHideEvent;
    }
    public void OnDialogueNextPushed()
    {
        NPC.CurrentlyPlayingNPC.ContinueDialogue();
    }

    public void OnChoice1Pushed()
    {
        NPC.CurrentlyPlayingNPC.ContinueDialogue(choices[0].transform.GetChild(0).GetComponent<Text>().text);
    }

    public void OnChoice2Pushed()
    {
        NPC.CurrentlyPlayingNPC.ContinueDialogue(choices[1].transform.GetChild(0).GetComponent<Text>().text);
    }
    public void OnChoice3Pushed()
    {
        NPC.CurrentlyPlayingNPC.ContinueDialogue(choices[2].transform.GetChild(0).GetComponent<Text>().text);
    }

    private void OnDialogueHideEvent()
    {
        dialogueWindow.SetActive(false);
    }

    void OnDialogueEvent(string speaker, string text, List<string> c)
    {
        dialogueWindow.SetActive(true);
        Speaker.text = speaker;
        MainDialogue.text = text;
        foreach (var i in choices)
        {
            i.SetActive(false);
        }
        if (c == null)
            return;
        for (int i = 0; i < c.Count; ++i)
        {
            if (choices.Count > i)
            {
                choices[i].SetActive(true);
                choices[i].transform.GetChild(0).GetComponent<Text>().text = c[i];
            }
        }
    }

}
