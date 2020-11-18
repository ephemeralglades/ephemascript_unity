using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ephemascript
{
    /// <summary>
    /// Represents an entity that will execute ephemascript code. Extend me!
    /// </summary>
    public class NPC : MonoBehaviour
    {
        [System.Serializable]
        public class NPCStateAsset
        {
            public string state;
            public TextAsset asset;
        }

        #region invokable events

        public static NPC CurrentlyPlayingNPC = null;
        /// <summary>
        /// Invoke this event when a choice has been made by the user
        /// </summary>
        public void ContinueDialogue(string choice = null)
        {
            this.getDialogEvent(choice);
        }
        #endregion

        #region subscribable events
        public delegate void DialogueTextEvent(string speaker, string text, List<string> choices);
        /// <summary>
        /// Invoked when a text event is received. Choices will be null if there are no choices to the 
        /// </summary>
        public static event DialogueTextEvent OnDialogueText;

        public delegate void DialogueHideEvent();
        /// <summary>
        /// Invoked when a hide event is received. Expect the UI to hide the chatbox when received
        /// </summary>
        public static event DialogueHideEvent OnDialogueHide;
        #endregion

        #region Public Members - Set Me!

        public string displayName = "";
        public List<NPCStateAsset> assets = new List<NPCStateAsset>();
        #endregion


        #region Virtual Members - Please Override Me !

        #region Unity Functions
        /// <summary>
        /// Initialize anything else here.
        /// </summary>
        public virtual void Start()
        {
            HashSet<KeyValuePair<string, string>> allVars = new HashSet<KeyValuePair<string, string>>();
            foreach (var asset in assets)
            {
                var a = _assets[asset.state] = new NPCTextParser(asset.asset, ConditionalChecker);
                allVars.UnionWith(a.getVars());
            }
            foreach (var v in allVars)
            {
                var ve = variables[v.Key] = new Progress.SavableStoreValue<string>(displayName + "_" + v.Key, v.Value);
                ve.Initialize();
            }
        }
        #endregion

        /// <summary>
        /// Call me when you interacted with this NPC!
        /// </summary>
        /// <param name="state">Which state to play</param>
        public virtual void Interact(string state = "")
        {
            if (playingDialogue)
            {
                return;
            }
            CurrentlyPlayingNPC = this;
            playingDialogue = true;
            interactionStack.Clear();
            afterComplete = "continue";
            beginInteractionFromAssets(state);
        }

        /// <summary>
        /// Check a condition. The base variable conditions are built in. Implement a custom condition here
        /// </summary>
        /// <param name="command">The command</param>
        /// <param name="parameter1">Parameter 1</param>
        /// <param name="parameter2">Parameter 1</param>
        /// <returns></returns>
        public virtual bool ConditionalChecker(string command, string parameter1, string parameter2)
        {
            print("checking condition: " + command + " " + parameter1 + " " + parameter2);
            if (command == "var")
            {
                if (parameter2 == "")
                {
                    return variables[parameter1].Get() != "";
                }
                return variables[parameter1].Get() == parameter2;
            }
            else if (command == "varlt")
            {
                return float.Parse(variables[parameter1].Get()) < float.Parse(parameter2);
            }
            else if (command == "vargt")
            {
                return float.Parse(variables[parameter1].Get()) > float.Parse(parameter2);
            }
            else if (command == "varExt")
            {
                // external var
                return Progress.S.store.ContainsKey(parameter1) && Progress.S.store[parameter1] == parameter2;
            }
            else if (command == "varExtgt")
            {
                return Progress.S.store.ContainsKey(parameter1) && float.Parse(Progress.S.store[parameter1]) > float.Parse(parameter2);
            }
            else if (command == "varExtlt")
            {
                return Progress.S.store.ContainsKey(parameter1) && float.Parse(Progress.S.store[parameter1]) < float.Parse(parameter2);
            }
            return false;
        }
        public virtual bool ShouldNPCInteractableAppear(string state, GameObject obj)
        {
            return true;
        }

        /* invokaable functions! */
        public virtual void GrantItem(string name, string amount = "")
        {
            Debug.Log("Granting item: " + name + "x" + amount);
        }

        public virtual void SetVariable(string name, string to)
        {
            variables[name].Set(to);
        }

        public void noop()
        {
        }
        #endregion

        #region private members
        Dictionary<string, NPCTextParser> _assets = new Dictionary<string, NPCTextParser>();
        protected Dictionary<string, Progress.SavableStoreValue<string>> variables = new Dictionary<string, Progress.SavableStoreValue<string>>();
        private Stack<KeyValuePair<Coroutine, string>> interactionStack = new Stack<KeyValuePair<Coroutine, string>>();
        string afterComplete = "continue";
        bool playingDialogue = false;
        private IEnumerator PlayInteractionFromAssets(string state)
        {
            NPCTextParser parser = _assets[state];
            parser.ResetToBeginning();
            List<string> nextLines = parser.retrieveNextLines();
            // we're gonna get choices or lines or custom commands
            string currentSpeaker = displayName;
            int infLoopCounter = 0;
            while (nextLines.Count > 0)
            {
                if (nextLines.Count == 0)
                {
                    // end of conversation
                    break;
                }
                string line = nextLines[0];
                print("parsing line: " + line);
                string dialogue = line;

                if (line[0] == '@')
                {
                    // this is a command..
                    string[] cmds = line.Substring(1).Split(' ');
                    // run a blocking coroutine
                    List<object> arguments = new List<object>();
                    for (int c = 1; c < cmds.Length; ++c)
                    {
                        if (cmds[c][0] == '!')
                        {
                            arguments.Add(cmds[c].Replace('_', ' ').Substring(1));
                        }
                        else
                        {
                            arguments.Add(cmds[c]);
                        }
                    }
                    if (cmds[0] == "end")
                    {
                        string afterCompleteAction = "continue";
                        if (cmds.Length > 1)
                        {
                            afterCompleteAction = cmds[1];
                        }
                        afterComplete = afterCompleteAction;
                        break;
                    }
                    else if (cmds[0] == "hidedg")
                    {
                        if (OnDialogueHide != null)
                            OnDialogueHide();
                    }
                    else if (cmds[0] == "runcr")
                    {
                        if (cmds.Length > 2 && cmds[2] == "donthide")
                        {
                        }
                        else
                        {
                            if (OnDialogueHide != null)
                                OnDialogueHide();
                        }
                        yield return StartCoroutine(cmds[1], arguments.ToArray().Skip(1));
                    }
                    else if (cmds[0] == "wait")
                    {
                        if (OnDialogueHide != null)
                            OnDialogueHide();
                        yield return new WaitForSeconds(float.Parse(cmds[1]));
                    }
                    else if (cmds[0] == "setvar")
                    {
                        // set a variable
                        SetVariable((string)arguments[0], (string)arguments[1]);
                    }
                    else if (cmds[0] == "setvarExt")
                    {
                        // set an external variable
                        Progress.S.store[(string)arguments[0]] = (string)arguments[1];
                    }
                    else if (cmds[0] == "PlayState")
                    {
                        var newstack = new KeyValuePair<Coroutine, string>(null, "continue");
                        interactionStack.Push(newstack);
                        var cr = StartCoroutine(PlayInteractionFromAssets(cmds[1]));
                        yield return cr;
                        if (afterComplete == "all")
                        {
                            break;
                        }
                    }
                    else
                    {
                        var methods = this.GetType().GetMethods();
                        foreach (var method in methods)
                        {
                            if (method.Name == cmds[0])
                            {
                                method.Invoke(this, arguments.ToArray());
                            }
                        }
                    }
                    goto goon;
                }

                if (line.Contains("\\") || line.Contains("¥"))
                {
                    string[] splitLine;
                    // line contains options
                    // dialogue speaker change
                    if (line.IndexOf('¥') != -1)
                    {
                        splitLine = line.Split('¥');
                    }
                    else
                    {
                        splitLine = line.Split('\\');
                    }
                    currentSpeaker = splitLine[0];
                    dialogue = splitLine[1].TrimStart();
                }
                List<string> choices = new List<string>();
                Dictionary<string, string> choiceMaps = new Dictionary<string, string>();
                if (nextLines.Count > 1)
                {
                    // it's a choice
                    for (int d = 1; d < nextLines.Count; ++d)
                    {
                        string curLine = nextLines[d];
                        string choicetext;
                        string choiceMarker;
                        string[] curLineSplit;
                        if (curLine.IndexOf('¥') != -1)
                        {
                            curLineSplit = curLine.Split('¥');
                        }
                        else
                        {
                            curLineSplit = curLine.Split('\\');
                        }
                        choicetext = curLineSplit[0];
                        choiceMarker = curLineSplit[1];
                        if (curLineSplit.Length > 2)
                        {
                            // conditional choice
                            string[] splitCondition = curLineSplit[2].Split(' ');
                            for (int i = 0; i < splitCondition.Length; ++i)
                            {
                                if (splitCondition[i][0] == '!')
                                {
                                    splitCondition[i] = splitCondition[i].Replace('_', ' ').Substring(1);
                                }
                            }
                            if (ConditionalChecker(splitCondition[0], splitCondition.Length > 1 ? splitCondition[1] : "", splitCondition.Length > 2 ? splitCondition[2] : ""))
                            {
                                choices.Add(choicetext);
                            }
                        }
                        else
                            choices.Add(choicetext);
                        choiceMaps[choicetext] = choiceMarker;
                    }

                    if (OnDialogueText != null)
                        OnDialogueText(currentSpeaker, dialogue, choices);
                    yield return WaitForDialogResponse();
                    parser.GoTo(choiceMaps[lastOptionEvent]);
                }
                else
                {
                    if (OnDialogueText != null)
                        OnDialogueText(currentSpeaker, dialogue, null);
                    yield return WaitForDialogueContinue();
                }
            goon:
                ++infLoopCounter;
                if (infLoopCounter == int.MaxValue)
                {
                    Debug.LogError("Possible infinite loop in ephemascript code: " + displayName);
                    yield break;
                }
                // yield return 1;
                nextLines = parser.retrieveNextLines();
            }
            if (interactionStack.Count == 1)
            {
                print("finished playing dialogue");
                playingDialogue = false;
                if (OnDialogueHide != null)
                    OnDialogueHide();
            }
            if (interactionStack.Count > 0)
                interactionStack.Pop();
        }

        private IEnumerator WaitForDialogResponse()
        {
            waitingForEvent = true;
            waitingForResponse = true;
            yield return new WaitUntil(() => waitingForResponse == false);
        }

        private IEnumerator WaitForDialogueContinue()
        {
            waitingForEvent = true;
            waitingForResponse = false;
            yield return new WaitUntil(() => waitingForEvent == false);
        }

        string lastOptionEvent;
        private void getDialogEvent(string eventName = null)
        {
            if (eventName != null)
            {
                lastOptionEvent = eventName;
                waitingForResponse = false;
            }
            waitingForEvent = false;
        }
        bool waitingForEvent = false;
        bool waitingForResponse = false;

        private void beginInteractionFromAssets(string state)
        {
            var cr = StartCoroutine(PlayInteractionFromAssets(state));
            if (interactionStack.Count > 1)
            {
                foreach (var s in interactionStack)
                {
                    if (s.Key != null)
                        StopCoroutine(s.Key);
                }
                interactionStack.Clear();
            }
            interactionStack.Push(new KeyValuePair<Coroutine, string>(cr, "continue"));
        }
        #endregion

    }
}
