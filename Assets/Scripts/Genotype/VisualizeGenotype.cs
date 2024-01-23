using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using System.IO;

[System.Serializable]
public class VisualizeGenotype
{
    public static string cgToDotString(CreatureGenotype cg, bool visualizeNeurons)
    //public static void cgToDotString(CreatureGenotype cg, bool visualizeNeurons)

    {

        string dotString = "digraph g {";


        if (!visualizeNeurons)
        {

            foreach (SegmentGenotype segment in cg.segments)
            {

                // declare all the nodes
                if (segment.id == 1)
                {
                    dotString += ("1 [label=\"segment 1 (root)\", color=\"#"
                        + $"{segment.r:X2}{segment.g:X2}{segment.b:X2}" + "\"]; ");

                }
                else if (segment.id >= 2)
                {
                    dotString += (segment.id.ToString() + " [label=\"segment " + segment.id.ToString() + "\", color =\"#"
                        + $"{segment.r:X2}{segment.g:X2}{segment.b:X2}" + "\"]; ");

                }
            }

            // add all the connections for each node
            foreach (SegmentGenotype segment in cg.segments)
            {
                foreach (SegmentConnectionGenotype connection in segment.connections)
                {
                    dotString += segment.id.ToString() + " -> " + connection.destination.ToString();

                    byte recursiveLimitOfConnection = cg.GetSegment(connection.destination).recursiveLimit;

                    if (recursiveLimitOfConnection > 1)
                    {
                        dotString += " [label=\"" + recursiveLimitOfConnection.ToString() + "\"]";
                    }

                    dotString += "; ";
                }
            }

        }

        else
        {
            // graph definitions 
            dotString += "compound = true;"
                      + "splines = \"polyline\";"
                      + "rankdir = \"LR\";"
                      + "1[shape = \"point\"];";


            // declare all the subgraphs and nodes 

            foreach (SegmentGenotype segment in cg.segments)
            {

                // list all the subgraphs

                // code here ....


                foreach (NeuronGenotype neuron in segment.neurons)
                {

                    // list all the neurons within each segment

                    // code here ...

                    string label = neuron.type.ToString();
                    dotString += "[label=\"" + label + "\"]";
                    

                }


               
            }



        }



        dotString += "}";

        return dotString;
        //CreatePngFromDot(dotString, "output.png");
        //UnityEngine.Debug.Log(dotString);

    }

    public static void CreatePngFromDot(string dotString, string outputPath)
    {
        // run which dot and replace this string with the output
        // TODO: Could replace with a new process
        string dotPath = "/opt/homebrew/bin/dot";

        // Create a temporary DOT file
        UnityEngine.Debug.Log(dotString);
        string dotFilePath = Path.Combine(Application.persistentDataPath, "temp.dot");
        File.WriteAllText(dotFilePath, dotString);

        // Start a new process to run the 'dot' command
        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            // Replace with CMD.exe for Windows, for Mac -> /bin/bash
            FileName = "/bin/bash",
            WorkingDirectory = Application.persistentDataPath,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            // This is the command we are running in terminal
            Arguments = "-c \"" + dotPath + " -Tpng \"temp.dot\" -o \"output.png\"" + "\""
        };

        Process process = new Process
        {
            StartInfo = startInfo
        };

        // Execute our process
        process.Start();
        //process.BeginOutputReadLine();

        // This is a sanity check, print our command to the log to check it's correct
        //UnityEngine.Debug.Log(process.StartInfo.Arguments);
        process.WaitForExit();

        // Error Checking
        if (process.ExitCode != 0)
        {
            UnityEngine.Debug.LogError($"Error: 'dot' command failed with exit code {process.ExitCode}");
        }


        // If we run hello world, uncomment these lines to read the standard output
        //string output = process.StandardOutput.ReadToEnd();
        //UnityEngine.Debug.Log(output);

        // Clean up: Delete the temporary DOT file
        File.Delete(dotFilePath);

    }
}
