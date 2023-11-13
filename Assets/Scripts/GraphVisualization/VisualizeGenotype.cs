using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
//using System.Runtime.Serialization.Formatters.Binary;

[System.Serializable]
public class VisualizeGenotype
{
    public static string cgToDotString(CreatureGenotype cg, bool visualizeNeurons)
    {
        // string dotString = "";
        // for each segment in cg,
        // append each connection to DOTString


        string dotString = "digraph g { a -> b -> c; b -> d -> c; }"; // temporary, for testing purposes rn

        return dotString;
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