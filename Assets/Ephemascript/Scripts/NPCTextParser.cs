using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Ephemascript
{
    /// <summary>
    /// Interpreter for Ephemascript
    /// </summary>
    public class NPCTextParser
    {
        #region Public Interface. Use me!
        /// <summary>
        /// Constructor for an ephemascript interpreter
        /// </summary>
        /// <param name="_text">Text asset representing the script</param>
        /// <param name="_conditionalFunc">Function to use for conditional logic checks</param>
        public NPCTextParser(TextAsset _text, Func<string, string, string, bool> _conditionalFunc)
        {
            conditionalFunc = _conditionalFunc;
            text = _text;
            int ifDepth = 0;
            int i = 0;
            int ifId = 0;
            Dictionary<int, int> depthToCurrentIf = new Dictionary<int, int>();
            foreach (var line in text.text.Split('\n'))
            {
                var lineT = line.Trim();
                if (lineT.StartsWith("//"))
                {
                    // this is a comment
                    continue;
                }
                if (lineT.StartsWith("@var"))
                {
                    string[] splitLine = lineT.Split(' ');
                    // this is a variable declaration
                    declaredVars.Add(new KeyValuePair<string, string>(splitLine[1], splitLine[2]));
                    continue;
                }
                if (lineT.StartsWith("@") && lineT.EndsWith(":"))
                {
                    // this is a marker
                    string marker = lineT.Remove(lineT.Length - 1, 1);
                    markers[marker.Substring(1)] = i;
                    continue;
                }
                lines.Add(lineT);
                if (lineT.StartsWith("@if"))
                {
                    ++ifDepth;
                    while (blockData.ContainsKey(ifId))
                    {
                        ++ifId;
                    }
                    depthToCurrentIf[ifDepth] = ifId;
                    var d = blockData[ifId] = new IfBlockData();
                    d.startIndex = i;
                    d.conditionsIndecies = new List<int>();
                }
                else if (lineT.StartsWith("@elseif"))
                {
                    blockData[ifId].conditionsIndecies.Add(i);
                }
                else if (lineT.StartsWith("@else"))
                {
                    blockData[ifId].elseIndex = i;
                }
                else if (lineT.StartsWith("@endif"))
                {
                    blockData[ifId].endIndex = i;
                    --ifDepth;
                    if (ifDepth > 0)
                    {
                        ifId = depthToCurrentIf[ifDepth];
                    }
                }
                if (ifDepth == 0)
                {
                    ifBlocks.Add(-1);
                }
                else
                    ifBlocks.Add(ifId);

                ++i;
            }
            ResetToBeginning();
        }

        /// <summary>
        /// Reset the interpreter to the beginning of the script.
        /// </summary>
        public void ResetToBeginning()
        {
            currentLine = 0;
        }

        /// <summary>
        /// Get a list of all the local variables used in this script
        /// </summary>
        /// <returns>Hash set of the variables</returns>
        public HashSet<KeyValuePair<string, string>> getVars()
        {
            return declaredVars;
        }

        /// <summary>
        /// manually jump to a marker in the script (Markers have syntax @MARKER:
        /// </summary>
        /// <param name="marker">name of the marker</param>
        public void GoTo(string marker)
        {
            currentLine = markers[marker];
        }

        /// <summary>
        /// Retrieve the next line of code to interpret. Call me after you have processed the previous line!
        /// </summary>
        /// <returns>The string value of the next line of code</returns>
        public List<string> retrieveNextLines()
        {
            List<string> nextLines = new List<string>();
            for (; currentLine < lines.Count; ++currentLine)
            {
            foundline:
                string cLine = lines[currentLine];
                if (cLine.StartsWith("@if"))
                {
                    // check the conditional
                    string[] splitLine = cLine.Split(' ');
                    for (int c = 0; c < splitLine.Length; ++c)
                    {
                        if (splitLine[c][0] == '!')
                        {
                            splitLine[c] = splitLine[c].Replace('_', ' ').Substring(1);
                        }
                    }
                    if (conditionalFunc(splitLine[1], splitLine.Length < 3 ? "" : splitLine[2], splitLine.Length < 4 ? "" : splitLine[3]))
                    {
                        // we're good, go to the next line!
                        currentLine += 2;
                        goto foundline;
                    }
                    else
                    {
                        // go to the next conditional
                        var blockD = blockData[ifBlocks[currentLine]];
                        foreach (var ci in blockD.conditionsIndecies)
                        {
                            cLine = lines[ci];
                            // test each conditional
                            string[] splitLine2 = cLine.Split(' ');
                            if (conditionalFunc(splitLine2[1], splitLine2.Length < 3 ? "" : splitLine2[2], splitLine2.Length < 4 ? "" : splitLine2[3]))
                            {
                                // we're good, go to this line!
                                currentLine = ci + 2;
                                goto foundline;
                            }
                        }
                        if (blockD.elseIndex > 0)
                        {
                            // go to the else index
                            currentLine = blockD.elseIndex + 2;
                            goto foundline;
                        }
                        else
                        {
                            // ok... guess we'll just go to the end of the if block
                            currentLine = blockD.endIndex + 2;
                            goto foundline;
                        }
                    }
                }
                if (cLine.StartsWith("@elseif") || cLine.StartsWith("@else") || cLine.StartsWith("@endif"))
                {
                    // we were in a block and we need to go to the end
                    var blockD = blockData[ifBlocks[currentLine - 1]];
                    currentLine = blockD.endIndex + 2;
                    goto foundline;
                }
                if (cLine.StartsWith("@goto"))
                {
                    string[] splitS = cLine.Split(' ');
                    currentLine = markers[splitS[1]];
                    goto foundline;
                }
                if (cLine == "")
                {
                    ++currentLine;
                    return nextLines;
                }
                nextLines.Add(cLine);
            }
            return nextLines;
        }

        #endregion

        #region Private members
        TextAsset text;
        int currentLine = 0;
        List<string> lines = new List<string>();
        List<int> ifBlocks = new List<int>();
        Dictionary<string, int> markers = new Dictionary<string, int>();
        class IfBlockData
        {
            public int startIndex;
            public List<int> conditionsIndecies;
            public int elseIndex;
            public int endIndex;
        }
        Dictionary<int, IfBlockData> blockData = new Dictionary<int, IfBlockData>();
        Func<string, string, string, bool> conditionalFunc;
        HashSet<KeyValuePair<string, string>> declaredVars = new HashSet<KeyValuePair<string, string>>();

        public void FailedCondition()
        {
            // go to an else or an endif
            for (; currentLine < lines.Count; ++currentLine)
            {
                string cLine = lines[currentLine];
                if (cLine == "")
                {
                    continue;
                }
                string[] splitline = cLine.Split(' ');
                if (splitline[0][0] == '@')
                {
                    if (splitline[0] == "@endif")
                    {
                        // we're good here!
                        currentLine += 2;
                        return;
                    }
                    if (splitline[0] == "@else")
                    {
                        if (splitline.Length == 1)
                        {
                            // we're also good here
                            currentLine += 2;
                            return;
                        }
                    }
                }
            }
        }

        #endregion

    }
}
