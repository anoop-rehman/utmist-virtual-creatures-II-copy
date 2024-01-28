using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using System.IO;

[System.Serializable]
public class VisualizeGenotype
{
    public static string ngTypeToString(NeuronGenotype neuron)
    {
        /*0 => a + b, // sum
            1 => a* b, // product
            2 => a / b, // divide
            3 => Mathf.Min(a + b, c), // sum-threshold
            4 => a > b ? 1 : -1, // greater-than
            5 => Mathf.Sign(a), //sign-of
            6 => Mathf.Min(a, b), // min
            7 => Mathf.Max(a, b), // max
            8 => Mathf.Abs(a), // abs
            9 => a > 0 ? b : c, // if
            10 => a + (b - a) * c, // interpolate
            11 => Mathf.Sin(a), // sin
            12 => Mathf.Cos(a), // cos
            13 => Mathf.Atan(a), // atan
            14 => Mathf.Log10(Mathf.Abs(a)), // log
            15 => Mathf.Exp(a), // expt
            16 => 1 / (1 + Mathf.Exp(-a)), // sigmoid
            17 => outValue + Time.deltaTime * ((a + dummy1) * 0.5f), // integrate
            18 => (a - dummy1) / Time.deltaTime, // differentiate
            19 => outValue + (a - outValue) * 0.5f, // smooth
            20 => a, // memory
            21 => b* Mathf.Sin(Time.time * a) + c, // oscillate-wave
            22 => b * (Time.time * a - Mathf.Floor(Time.time * a)) + c, // oscillate-saw
            _ => 0
        */

        switch (neuron.type)
        {
            case 0: return "sum";
            case 1: return "prod";
            case 2: return "div";
            case 3: return "sumt";
            case 4: return ">";
            case 5: return "sign";
            case 6: return "min";
            case 7: return "max";
            case 8: return "abs";
            case 9: return "if";
            case 10: return "itplt";
            case 11: return "sin";
            case 12: return "cos";
            case 13: return "atan";
            case 14: return "log";
            case 15: return "expt";
            case 16: return "sigm";
            case 17: return "intgrl";
            case 18: return "d/dx";
            case 19: return "smooth";
            case 20: return "mem";
            case 21: return "wav";
            case 22: return "saw";
            default: return "";
        }
    }

    public static string cgToDotString(CreatureGenotype cg, bool visualizeNeurons)
    //public static void cgToDotString(CreatureGenotype cg, bool visualizeNeurons)

    {

        string dotString = "digraph \"" + cg.name + "\" {";


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
                        dotString += " [label=\"" + recursiveLimitOfConnection.ToString() + "\"]";
                    }

                    dotString += "; ";
                }
            }


            dotString += "}";
        }
        else
        {
            // graph definitions 
            dotString += "compound = true;"
                      + "splines = \"polyline\";"
                      + "rankdir = \"LR\";\n";
                      //+ "1[shape = \"point\"];";

            int count = 0;

            // this list holds what #label neuron is the first neuron in a segment
            List<int> firstNeur = new List<int>(); 

            // declare all the subgraphs and nodes 

            foreach (SegmentGenotype segment in cg.segments)
            {

                // list all the subgraphs
                dotString += "subgraph cluster" + segment.id.ToString() + " {\n";

                if (segment.id == 0)
                {
                    dotString += "graph [style=dashed];\n";
                }

                firstNeur.Add(count);

                if (segment.neurons.Count() == 0)
                {
                    dotString += count.ToString() + " [label=\"\", color=\"#FFFFFF\"];\n";
                    count++;
                }

                foreach (NeuronGenotype neuron in segment.neurons)
                {
                    // list all the neurons within each segment
                    string label = ngTypeToString(neuron);
                    dotString += count.ToString() + " [label=\"" + label + "\"];\n";
                    count++;
                }

                dotString += "}\n";
            }

            int recCount = count + 1;

            foreach (SegmentGenotype segment in cg.segments)
            {
                foreach (SegmentConnectionGenotype connection in segment.connections)
                {
                    if (segment.id == connection.destination)
                    {
                        dotString += recCount.ToString() + "[shape = \"point\"];\n";
                        dotString += firstNeur[segment.id].ToString() + " -> " + recCount.ToString();
                        dotString += " [ltail=cluster" + connection.destination + "];\n";
                        dotString += recCount.ToString() + " -> " + firstNeur[connection.destination].ToString();
                        dotString += " [lhead=cluster" + connection.destination + "];\n";
                        recCount++;

                        //continue;
                    } else
                    {
                        dotString += firstNeur[segment.id].ToString() + " -> " + firstNeur[connection.destination].ToString();
                        dotString += " [ltail=cluster" + segment.id + ",lhead=cluster" + connection.destination + "];\n";
                    }
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
