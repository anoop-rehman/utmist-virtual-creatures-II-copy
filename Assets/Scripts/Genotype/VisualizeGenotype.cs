using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

[System.Serializable]
public class VisualizeGenotype
{
    //public static string cgToDotString(CreatureGenotype cg, bool visualizeNeurons)
    public static void cgToDotString(CreatureGenotype cg, bool visualizeNeurons)

    {

        string dotString = "digraph g {";


        if (!visualizeNeurons)
        {

            foreach (SegmentGenotype segment in cg.segments)
            {

                // list all the nodes
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
                        dotString += " [label=" + recursiveLimitOfConnection.ToString() + "\"]";
                    }

                    dotString += "; ";
                }
            }


            dotString += "}";
        }
        else
        {
            {}
        }

        //return dotString;
        Debug.Log(dotString);

    }


    //public void CreatePngFromDot(string dotString, string outputPath)
    //{
    //    // Command to run Graphviz
    //    string command = "dot";

    //    // Arguments for the command
    //    string args = "-Tpng -o \"" + outputPath + "\"";

    //    // Start a new process
    //    ProcessStartInfo startInfo = new ProcessStartInfo(command, args)
    //    {
    //        RedirectStandardInput = true,
    //        UseShellExecute = false
    //    };

    //    Process process = Process.Start(startInfo);

    //    // Write the DOT string to the standard input of the process
    //    using (StreamWriter writer = process.StandardInput)
    //    {
    //        writer.WriteLine(dotString);
    //    }

    //    process.WaitForExit();
    //}
}